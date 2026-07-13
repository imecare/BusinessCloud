using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.UpdateBzaSoldProduct;

public class UpdateBzaSoldProductHandler(IBazaresDbContext context, IMongoContext mongoContext)
    : IRequestHandler<UpdateBzaSoldProductCommand, bool>
{
    private readonly IBazaresDbContext _context = context;
    private readonly IMongoContext _mongoContext = mongoContext;

    public async Task<bool> Handle(UpdateBzaSoldProductCommand request, CancellationToken cancellationToken)
    {
        var soldProduct = await _context.SoldProducts
            .Include(p => p.Sale)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (soldProduct is null) return false;

        // Si la venta ya fue incluida en un envío de totales (evento en proceso de pago),
        // no se puede modificar para no alterar los totales enviados al cliente.
        if (soldProduct.Sale.BzaClosureEventId is not null)
            throw new InvalidOperationException(
                "El evento ya está en proceso de pago (se enviaron totales a los clientes). No se pueden modificar ventas de este evento.");

        var oldPrice = soldProduct.Price;

        soldProduct.Description = request.Description;
        soldProduct.Price = request.Price;

        await _context.SaveChangesAsync(cancellationToken);

        await _mongoContext.InsertAuditLogAsync(new
        {
            Event = "Bza_SoldProductUpdated",
            SoldProductId = soldProduct.Id,
            SaleEventId = soldProduct.Sale.BzaEventId,
            CustomerId = soldProduct.Sale.BzaCustomerId,
            OldPrice = oldPrice,
            NewPrice = request.Price,
            Timestamp = DateTime.UtcNow
        }, cancellationToken);

        return true;
    }
}
