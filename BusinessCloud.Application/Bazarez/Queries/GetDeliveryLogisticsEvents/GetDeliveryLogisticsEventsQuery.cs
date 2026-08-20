using BusinessCloud.Application.Bazares.Queries.GetClosureEvents;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace BusinessCloud.Application.Bazares.Queries.GetDeliveryLogisticsEvents;
public record GetDeliveryLogisticsEventsQuery : IRequest<List<ClosureEventListItemDto>>;

public class GetDeliveryLogisticsEventsHandler(IBazaresDbContext context)
    : IRequestHandler<GetDeliveryLogisticsEventsQuery, List<ClosureEventListItemDto>>
{
    public async Task<List<ClosureEventListItemDto>> Handle(
        GetDeliveryLogisticsEventsQuery request,
        CancellationToken ct)
    {
        return await context.ClosureEvents
            .AsNoTracking()
            .Where(c =>
                c.Status != BzaClosureEventStatus.Cancelled
                && !c.InDeliveryProcess
                && !c.Delivered
                && c.CustomerTotals.Any(t => t.Status == BzaClosureCustomerTotalStatus.Validated)
                && context.Sales.Any(s => s.BzaClosureEventId == c.Id))
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new ClosureEventListItemDto(
                c.Id,
                c.Description,
                c.OfficialDeliveryDate,
                c.PaymentDeadline,
                c.Status,
                c.InDeliveryProcess,
                c.Delivered,
                c.CreatedAt,
                c.CustomerTotals.Count,
                c.CustomerTotals.Count(t => t.Status == BzaClosureCustomerTotalStatus.ProofReceived),
                c.CustomerTotals.Count(t => t.Status == BzaClosureCustomerTotalStatus.Validated),
                c.CustomerTotals.Sum(t => (decimal?)t.TotalAmount) ?? 0m,
                true))
            .ToListAsync(ct);
    }
}