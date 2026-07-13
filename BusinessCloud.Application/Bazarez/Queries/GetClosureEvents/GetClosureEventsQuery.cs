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
    DateTime CreatedAt,
    int CustomerCount,
    int ProofsReceived,
    decimal TotalAmount);

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
                c.CreatedAt,
                c.CustomerTotals.Count,
                c.CustomerTotals.Count(t => t.Status == 2),
                c.CustomerTotals.Sum(t => (decimal?)t.TotalAmount) ?? 0m))
            .ToListAsync(cancellationToken);
    }
}
