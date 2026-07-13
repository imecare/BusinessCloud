using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.ReactivateClosureSale;

public class ReactivateClosureSaleHandler(IBazaresDbContext context)
    : IRequestHandler<ReactivateClosureSaleCommand, ReactivateClosureSaleResultDto>
{
    private readonly IBazaresDbContext _context = context;

    public async Task<ReactivateClosureSaleResultDto> Handle(ReactivateClosureSaleCommand request, CancellationToken cancellationToken)
    {
        var total = await _context.ClosureCustomerTotals
            .Include(t => t.ClosureEvent)
                .ThenInclude(c => c.Items)
            .Include(t => t.Customer)
            .FirstOrDefaultAsync(t => t.Id == request.ClosureCustomerTotalId, cancellationToken)
            ?? throw new KeyNotFoundException("El total del cliente no existe.");

        if (total.Status != BzaClosureCustomerTotalStatus.Cancelled)
            throw new InvalidOperationException("Solo se pueden reactivar ventas canceladas.");

        // Ventas del cliente en el cierre actual y sus eventos de venta.
        var sales = await _context.Sales
            .Where(s => s.BzaClosureEventId == total.BzaClosureEventId
                        && s.BzaCustomerId == total.BzaCustomerId)
            .ToListAsync(cancellationToken);
        var saleEventIds = sales.Select(s => s.BzaEventId).Distinct().ToList();

        // Reactivar: volver a Pendiente y limpiar datos de cancelación/rechazo.
        total.Status = BzaClosureCustomerTotalStatus.Pending;
        total.CancellationReason = null;
        total.CancelledIsCustomerFault = null;
        total.CancelledAt = null;
        total.RejectionReason = null;

        if (request.Mode == ReactivateMode.Existing)
        {
            if (!request.TargetClosureEventId.HasValue)
                throw new ArgumentException("Debes indicar el evento de pago destino.");

            var target = await _context.ClosureEvents
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == request.TargetClosureEventId.Value, cancellationToken)
                ?? throw new KeyNotFoundException("El evento de pago destino no existe.");

            if (target.Status == BzaClosureEventStatus.Cancelled)
                throw new InvalidOperationException("El evento de pago destino está cancelado.");

            MoveToClosure(total, sales, saleEventIds, target);

            if (target.Status == BzaClosureEventStatus.Validated)
                target.Status = BzaClosureEventStatus.PendingPayment;
        }
        else if (request.Mode == ReactivateMode.New)
        {
            if (!request.NewDeliveryDate.HasValue || !request.NewPaymentDeadline.HasValue)
                throw new ArgumentException("Debes indicar la fecha de entrega y la fecha límite de pago del nuevo evento.");

            var customerName = total.Customer?.Name ?? "Cliente";
            var newClosure = new BzaClosureEvent
            {
                Description = $"Reactivación · {customerName} — Entrega {request.NewDeliveryDate.Value:dd/MM/yyyy}",
                OfficialDeliveryDate = request.NewDeliveryDate,
                PaymentDeadline = request.NewPaymentDeadline.Value,
                Status = BzaClosureEventStatus.PendingPayment,
                Items = saleEventIds.Select(id => new BzaClosureEventItem { BzaEventId = id }).ToList()
            };
            if (total.BzaCollectorGroupId.HasValue)
            {
                newClosure.GroupDeliveries.Add(new BzaClosureGroupDelivery
                {
                    BzaCollectorGroupId = total.BzaCollectorGroupId.Value,
                    DeliveryDate = request.NewDeliveryDate.Value
                });
            }

            _context.ClosureEvents.Add(newClosure);
            await _context.SaveChangesAsync(cancellationToken);

            // Reasignar el total y sus ventas al nuevo cierre.
            total.BzaClosureEventId = newClosure.Id;
            foreach (var sale in sales)
                sale.BzaClosureEventId = newClosure.Id;
        }
        // Mode.Same: se mantiene en el cierre actual.

        await _context.SaveChangesAsync(cancellationToken);

        var resultClosureId = total.BzaClosureEventId;
        var closureStatus = await _context.ClosureEvents
            .Where(c => c.Id == resultClosureId)
            .Select(c => c.Status)
            .FirstOrDefaultAsync(cancellationToken);

        return new ReactivateClosureSaleResultDto
        {
            ClosureEventId = resultClosureId,
            ClosureCustomerTotalId = total.Id,
            TotalStatus = total.Status,
            ClosureStatus = closureStatus
        };
    }

    /// <summary>
    /// Mueve el total y sus ventas a un cierre existente, agregando los items
    /// (eventos de venta) que le falten al destino para que el flujo de pago funcione.
    /// </summary>
    private void MoveToClosure(
        BzaClosureCustomerTotal total,
        List<BzaSale> sales,
        List<int> saleEventIds,
        BzaClosureEvent target)
    {
        var existingItemEventIds = target.Items.Select(i => i.BzaEventId).ToHashSet();
        foreach (var eventId in saleEventIds.Where(id => !existingItemEventIds.Contains(id)))
        {
            target.Items.Add(new BzaClosureEventItem
            {
                BzaClosureEventId = target.Id,
                BzaEventId = eventId
            });
        }

        total.BzaClosureEventId = target.Id;
        foreach (var sale in sales)
            sale.BzaClosureEventId = target.Id;
    }
}
