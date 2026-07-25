using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.MovePendingSales;

public class MovePendingSalesHandler(IBazaresDbContext context)
    : IRequestHandler<MovePendingSalesCommand, MovePendingSalesResultDto>
{
    private readonly IBazaresDbContext _context = context;

    public async Task<MovePendingSalesResultDto> Handle(MovePendingSalesCommand request, CancellationToken cancellationToken)
    {
        var totals = await _context.ClosureCustomerTotals
            .Where(t => t.BzaClosureEventId == request.ClosureEventId
                        && t.Status == BzaClosureCustomerTotalStatus.Pending)
            .ToListAsync(cancellationToken);

        if (totals.Count == 0)
            return new MovePendingSalesResultDto { MovedCount = 0, TargetClosureEventId = request.ClosureEventId };

        var customerIds = totals.Select(t => t.BzaCustomerId).Distinct().ToList();
        var sales = await _context.Sales
            .Where(s => s.BzaClosureEventId == request.ClosureEventId && customerIds.Contains(s.BzaCustomerId))
            .ToListAsync(cancellationToken);
        var saleEventIds = sales.Select(s => s.BzaEventId).Distinct().ToList();
        var groupIds = totals.Where(t => t.BzaCollectorGroupId.HasValue)
            .Select(t => t.BzaCollectorGroupId!.Value)
            .Distinct()
            .ToList();

        int targetId;

        if (request.Mode == MovePendingSalesMode.Existing)
        {
            if (!request.TargetClosureEventId.HasValue)
                throw new ArgumentException("Debes indicar el evento de pago destino.");

            var target = await _context.ClosureEvents
                .Include(c => c.Items)
                .Include(c => c.GroupDeliveries)
                .FirstOrDefaultAsync(c => c.Id == request.TargetClosureEventId.Value, cancellationToken)
                ?? throw new KeyNotFoundException("El evento de pago destino no existe.");

            if (target.Status == BzaClosureEventStatus.Cancelled)
                throw new InvalidOperationException("El evento de pago destino está cancelado.");
            if (target.InDeliveryProcess)
                throw new InvalidOperationException("El evento de pago destino ya está en proceso de entrega.");

            var existingItemEventIds = target.Items.Select(i => i.BzaEventId).ToHashSet();
            foreach (var eventId in saleEventIds.Where(id => !existingItemEventIds.Contains(id)))
            {
                target.Items.Add(new BzaClosureEventItem
                {
                    BzaClosureEventId = target.Id,
                    BzaEventId = eventId
                });
            }

            foreach (var total in totals)
                total.BzaClosureEventId = target.Id;
            foreach (var sale in sales)
                sale.BzaClosureEventId = target.Id;

            if (target.Status == BzaClosureEventStatus.Validated)
                target.Status = BzaClosureEventStatus.PendingPayment;

            targetId = target.Id;
        }
        else
        {
            if (!request.NewDeliveryDate.HasValue || !request.NewPaymentDeadline.HasValue)
                throw new ArgumentException("Debes indicar la fecha de entrega y la fecha límite de pago del nuevo evento.");

            var newClosure = new BzaClosureEvent
            {
                Description = $"Pendientes movidos — Entrega {request.NewDeliveryDate.Value:dd/MM/yyyy}",
                OfficialDeliveryDate = request.NewDeliveryDate,
                PaymentDeadline = request.NewPaymentDeadline.Value,
                Status = BzaClosureEventStatus.PendingPayment,
                Items = saleEventIds.Select(id => new BzaClosureEventItem { BzaEventId = id }).ToList(),
                GroupDeliveries = groupIds.Select(gid => new BzaClosureGroupDelivery
                {
                    BzaCollectorGroupId = gid,
                    DeliveryDate = request.NewDeliveryDate.Value
                }).ToList()
            };

            _context.ClosureEvents.Add(newClosure);
            await _context.SaveChangesAsync(cancellationToken);

            foreach (var total in totals)
                total.BzaClosureEventId = newClosure.Id;
            foreach (var sale in sales)
                sale.BzaClosureEventId = newClosure.Id;

            targetId = newClosure.Id;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new MovePendingSalesResultDto
        {
            MovedCount = totals.Count,
            TargetClosureEventId = targetId
        };
    }
}