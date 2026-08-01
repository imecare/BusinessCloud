using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using BusinessCloud.Domain.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.SaveLiveSaleRow;

public class SaveLiveSaleRowHandler(IBazaresDbContext context, IMongoContext mongoContext)
    : IRequestHandler<SaveLiveSaleRowCommand, SaveLiveSaleRowResult>
{
    public async Task<SaveLiveSaleRowResult> Handle(SaveLiveSaleRowCommand request, CancellationToken ct)
    {
        var saleEvent = await context.Events.FirstOrDefaultAsync(x => x.Id == request.BzaEventId, ct)
            ?? throw new KeyNotFoundException("Evento de Venta no encontrado.");
        if (saleEvent.Status != 1)
            throw new InvalidOperationException("El evento no esta abierto para capturar ventas.");

        if (request.BzaCustomerId is null)
        {
            var draft = request.DraftId is null
                ? new BzaLiveSaleDraft { BzaEventId = request.BzaEventId }
                : await context.LiveSaleDrafts.FirstOrDefaultAsync(
                    x => x.Id == request.DraftId && x.BzaEventId == request.BzaEventId, ct)
                    ?? throw new KeyNotFoundException("Producto pendiente no encontrado.");

            draft.Description = request.Description.Trim();
            draft.Price = request.Price;
            if (request.DraftId is null) context.LiveSaleDrafts.Add(draft);
            await context.SaveChangesAsync(ct);
            return new SaveLiveSaleRowResult { DraftId = draft.Id, Assigned = false };
        }

        var customer = await context.Customers
            .Include(x => x.Collector)
                .ThenInclude(x => x.CollectorGroup)
            .FirstOrDefaultAsync(x => x.Id == request.BzaCustomerId, ct)
            ?? throw new KeyNotFoundException("Cliente no encontrado.");
        EnsureCollectorActive(customer);

        var sale = await context.Sales.Include(x => x.Products).FirstOrDefaultAsync(
            x => x.BzaEventId == request.BzaEventId && x.BzaCustomerId == request.BzaCustomerId, ct);
        if (sale?.IsClosed == true)
            throw new InvalidOperationException("La venta esta cerrada y no admite mas productos.");

        sale ??= new BzaSale { BzaEventId = request.BzaEventId, BzaCustomerId = customer.Id };
        if (sale.Id == 0) context.Sales.Add(sale);

        var product = new BzaSoldProduct
        {
            Description = request.Description.Trim(),
            Price = request.Price,
        };
        sale.Products.Add(product);

        if (request.DraftId is not null)
        {
            var draft = await context.LiveSaleDrafts.FirstOrDefaultAsync(
                x => x.Id == request.DraftId && x.BzaEventId == request.BzaEventId, ct)
                ?? throw new KeyNotFoundException("Producto pendiente no encontrado.");
            context.LiveSaleDrafts.Remove(draft);
        }

        await context.SaveChangesAsync(ct);
        await mongoContext.InsertAuditLogAsync(new
        {
            Event = "Bza_LiveSaleRowAssigned",
            SaleEventId = saleEvent.Id,
            CustomerId = customer.Id,
            ProductId = product.Id,
            Timestamp = DateTime.UtcNow,
        }, ct);

        return new SaveLiveSaleRowResult { SoldProductId = product.Id, Assigned = true };
    }

    private static void EnsureCollectorActive(BzaCustomer customer)
    {
        var collector = customer.Collector;
        if (collector is null) return;
        var group = collector.CollectorGroup;
        if (!collector.IsActive)
            throw new SaleCollectorInactiveException(
                $"El recolector '{collector.Name}' esta inactivo.", "COLLECTOR_INACTIVE",
                collector.Id, collector.Name, true, group?.Id, group?.Description, group is not null && !group.IsActive);
        if (group is not null && !group.IsActive)
            throw new SaleCollectorInactiveException(
                $"El grupo '{group.Description}' esta inactivo.", "COLLECTOR_GROUP_INACTIVE",
                collector.Id, collector.Name, false, group.Id, group.Description, true);
    }
}
