using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.DeleteClosureDraft;

public class DeleteClosureDraftHandler(IBazaresDbContext context)
    : IRequestHandler<DeleteClosureDraftCommand>
{
    private readonly IBazaresDbContext _context = context;

    public async Task Handle(DeleteClosureDraftCommand request, CancellationToken cancellationToken)
    {
        var closure = await _context.ClosureEvents
            .Include(c => c.CustomerTotals)
            .FirstOrDefaultAsync(c => c.Id == request.ClosureEventId, cancellationToken)
            ?? throw new KeyNotFoundException("El cierre no existe.");

        if (closure.InDeliveryProcess || closure.Delivered)
            throw new InvalidOperationException("Este cierre ya est? en proceso de entrega o finalizado y no se puede cancelar como draft.");

        var totalIds = closure.CustomerTotals.Select(t => t.Id).ToList();

        var hasWhatsAppDispatch = totalIds.Count > 0 && await _context.WhatsAppMessages
            .AnyAsync(m => m.BzaClosureCustomerTotalId.HasValue && totalIds.Contains(m.BzaClosureCustomerTotalId.Value), cancellationToken);

        var hasNotificationDispatch = await _context.NotificationLogs
            .AnyAsync(l => l.BzaClosureEventId == closure.Id, cancellationToken);

        if (hasWhatsAppDispatch || hasNotificationDispatch)
            throw new InvalidOperationException("Este cierre ya tiene notificaciones enviadas y no puede eliminarse como draft.");

        var hasValidatedProgress = closure.CustomerTotals.Any(t =>
            t.Status == BzaClosureCustomerTotalStatus.ProofReceived
            || t.Status == BzaClosureCustomerTotalStatus.Validated);

        if (hasValidatedProgress)
            throw new InvalidOperationException("Este cierre ya tiene comprobantes en proceso o validados y no puede eliminarse como draft.");

        _context.ClosureEvents.Remove(closure);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
