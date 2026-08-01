using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using BusinessCloud.Domain.Common.Exceptions;
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
                .ThenInclude(s => s.Products)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (soldProduct is null) return false;

        if (soldProduct.Sale.BzaClosureEventId is not null)
            throw new InvalidOperationException(
                "El evento ya está en proceso de pago (se enviaron totales a los clientes). No se pueden modificar ventas de este evento.");

        var oldPrice = soldProduct.Price;
        var oldCustomerId = soldProduct.Sale.BzaCustomerId;
        var saleEventId = soldProduct.Sale.BzaEventId;

        if (request.BzaCustomerId is int customerId && customerId != oldCustomerId)
        {
            var customer = await _context.Customers
                .Include(x => x.Collector)
                    .ThenInclude(x => x.CollectorGroup)
                .FirstOrDefaultAsync(x => x.Id == customerId, cancellationToken)
                ?? throw new KeyNotFoundException("Cliente no encontrado.");
            EnsureCollectorActive(customer);

            var sourceSale = soldProduct.Sale;
            var targetSale = await _context.Sales
                .Include(x => x.Products)
                .FirstOrDefaultAsync(
                    x => x.BzaEventId == saleEventId && x.BzaCustomerId == customerId,
                    cancellationToken);
            if (targetSale?.IsClosed == true || targetSale?.BzaClosureEventId is not null)
                throw new InvalidOperationException("La venta del cliente destino está cerrada y no admite modificaciones.");

            targetSale ??= new BzaSale { BzaEventId = saleEventId, BzaCustomerId = customerId };
            if (targetSale.Id == 0) _context.Sales.Add(targetSale);

            sourceSale.Products.Remove(soldProduct);
            targetSale.Products.Add(soldProduct);
            if (sourceSale.Products.Count == 0 && !sourceSale.IsClosed)
                _context.Sales.Remove(sourceSale);
        }

        soldProduct.Description = request.Description.Trim();
        soldProduct.Price = request.Price;

        await _context.SaveChangesAsync(cancellationToken);

        await _mongoContext.InsertAuditLogAsync(new
        {
            Event = "Bza_SoldProductUpdated",
            SoldProductId = soldProduct.Id,
            SaleEventId = saleEventId,
            OldCustomerId = oldCustomerId,
            CustomerId = request.BzaCustomerId ?? oldCustomerId,
            OldPrice = oldPrice,
            NewPrice = request.Price,
            Timestamp = DateTime.UtcNow
        }, cancellationToken);

        return true;
    }

    private static void EnsureCollectorActive(BzaCustomer customer)
    {
        var collector = customer.Collector;
        if (collector is null) return;
        var group = collector.CollectorGroup;
        if (!collector.IsActive)
            throw new SaleCollectorInactiveException(
                $"El recolector '{collector.Name}' está inactivo.", "COLLECTOR_INACTIVE",
                collector.Id, collector.Name, true, group?.Id, group?.Description, group is not null && !group.IsActive);
        if (group is not null && !group.IsActive)
            throw new SaleCollectorInactiveException(
                $"El grupo '{group.Description}' está inactivo.", "COLLECTOR_GROUP_INACTIVE",
                collector.Id, collector.Name, false, group.Id, group.Description, true);
    }
}