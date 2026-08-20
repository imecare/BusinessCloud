using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetClosureEvents;

/// <summary>
/// Lista los Eventos de Cierre de Venta (historial de envíos de totales).
/// </summary>
public record GetClosureEventsQuery() : IRequest<List<ClosureEventListItemDto>>;

public record ClosureEventListItemDto(
    int Id,
    string Description,
    DateTime? OfficialDeliveryDate,
    DateTime PaymentDeadline,
    int Status,
    bool InDeliveryProcess,
    bool Delivered,
    Guid? DeliveryBatchId,
    DateTime CreatedAt,
    int CustomerCount,
    int ProofsReceived,
    int ValidatedCount,
    decimal TotalAmount,
    bool TotalsSent);

public class GetClosureEventsHandler(IBazaresDbContext context)
    : IRequestHandler<GetClosureEventsQuery, List<ClosureEventListItemDto>>
{
    private readonly IBazaresDbContext _context = context;

    public async Task<List<ClosureEventListItemDto>> Handle(GetClosureEventsQuery request, CancellationToken cancellationToken)
    {
        return await _context.ClosureEvents
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new ClosureEventListItemDto(
                c.Id,
                c.Description,
                c.OfficialDeliveryDate,
                c.PaymentDeadline,
                c.Status,
                c.InDeliveryProcess,
                c.Delivered,
                c.DeliveryBatchId,
                c.CreatedAt,
                c.CustomerTotals.Count,
                c.CustomerTotals.Count(t => t.Status == 2),
                c.CustomerTotals.Count(t => t.Status == 3),
                c.CustomerTotals.Sum(t => (decimal?)t.TotalAmount) ?? 0m,
                // "Totales enviados": el cierre dejó de ser draft porque ya entró a entrega,
                // hay progreso de comprobantes, o se despacharon notificaciones (WhatsApp/app).
                // Es el inverso de las condiciones de DeleteClosureDraft.
                c.InDeliveryProcess
                    || c.Delivered
                    || c.CustomerTotals.Any(t => t.Status == 2 || t.Status == 3)
                    || c.CustomerTotals.Any(t => _context.WhatsAppMessages
                        .Any(m => m.BzaClosureCustomerTotalId == t.Id))
                    || _context.NotificationLogs.Any(l => l.BzaClosureEventId == c.Id)))
            .ToListAsync(cancellationToken);
    }
}
