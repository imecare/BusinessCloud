using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.DeleteBzaSoldProduct;

public class DeleteBzaSoldProductHandler(IBazaresDbContext context, IMongoContext mongoContext)
    : IRequestHandler<DeleteBzaSoldProductCommand, bool>
{
    private readonly IBazaresDbContext _context = context;
    private readonly IMongoContext _mongoContext = mongoContext;

    public async Task<bool> Handle(DeleteBzaSoldProductCommand request, CancellationToken cancellationToken)
    {
        var soldProduct = await _context.SoldProducts
            .Include(p => p.Sale)
                .ThenInclude(s => s.Products)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (soldProduct is null) return false;

        var sale = soldProduct.Sale;

        // Si la venta ya fue incluida en un envío de totales (evento en proceso de pago),
        // no se puede modificar/eliminar para no alterar los totales enviados al cliente.
        if (sale.BzaClosureEventId is not null)
            throw new InvalidOperationException(
                "El evento ya está en proceso de pago (se enviaron totales a los clientes). No se pueden eliminar ventas de este evento.");

        var saleEventId = sale.BzaEventId;
        var customerId = sale.BzaCustomerId;
        var description = soldProduct.Description;
        var price = soldProduct.Price;

        _context.SoldProducts.Remove(soldProduct);

        // Si la venta queda SIN productos, eliminarla para no dejar al cliente
        // asociado al evento sin ventas reales. No se elimina si la venta ya fue
        // cerrada (comprobante subido) o incluida en un envío de totales (cierre).
        var remainingProducts = sale.Products.Count(p => p.Id != soldProduct.Id);
        var saleRemoved = false;
        if (remainingProducts == 0 && !sale.IsClosed && sale.BzaClosureEventId is null)
        {
            _context.Sales.Remove(sale);
            saleRemoved = true;
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _mongoContext.InsertAuditLogAsync(new
        {
            Event = "Bza_SoldProductDeleted",
            SoldProductId = request.Id,
            SaleEventId = saleEventId,
            CustomerId = customerId,
            Description = description,
            Price = price,
            SaleRemoved = saleRemoved,
            Timestamp = DateTime.UtcNow
        }, cancellationToken);

        return true;
    }
}
