using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetAllBzaSales;

public class GetAllBzaSalesHandler(IBazaresDbContext context)
    : IRequestHandler<GetAllBzaSalesQuery, List<BzaSaleListDto>>
{
    private readonly IBazaresDbContext _context = context;

    private static readonly Dictionary<int, string> StatusNames = new()
    {
        { 1, "Abierto" },
        { 2, "En proceso de pago" },
        { 3, "En Entrega" },
        { 4, "Finalizado" },
        { 5, "Cancelado" },
        { 6, "Finalizado" }
    };

    public async Task<List<BzaSaleListDto>> Handle(GetAllBzaSalesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Events.AsQueryable();

        if (request.FromDate.HasValue)
        {
            var from = request.FromDate.Value.Date;
            query = query.Where(s => s.CreatedAt >= from);
        }

        if (request.ToDate.HasValue)
        {
            var toExclusive = request.ToDate.Value.Date.AddDays(1);
            query = query.Where(s => s.CreatedAt < toExclusive);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(s => EF.Functions.Like(s.Description, $"%{term}%"));
        }

        var rawSales = await query
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new
            {
                s.Id,
                s.Description,
                s.PaymentDeadline,
                s.Status,
                TotalEventSales = s.Sales.SelectMany(x => x.Products).Sum(p => (decimal?)p.Price) ?? 0m,
                UnsentSalesAmount = s.Sales.Where(x => x.BzaClosureEventId == null).SelectMany(x => x.Products).Sum(p => (decimal?)p.Price) ?? 0m,
                HasSentSales = s.Sales.Any(x => x.BzaClosureEventId != null),
                UniqueCustomersCount = s.Sales.Select(x => x.BzaCustomerId).Distinct().Count(),
                TotalPaid = s.Payments.Where(p => p.IsVerified).Sum(p => (decimal?)p.Amount) ?? 0m,
                s.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var eventIds = rawSales.Select(s => s.Id).ToList();

        var closureLinks = await _context.ClosureEventItems
            .AsNoTracking()
            .Where(i => eventIds.Contains(i.BzaEventId))
            .Join(
                _context.ClosureEvents.AsNoTracking(),
                item => item.BzaClosureEventId,
                closure => closure.Id,
                (item, closure) => new
                {
                    item.BzaEventId,
                    closure.Id,
                    closure.Status,
                    closure.InDeliveryProcess,
                    closure.Delivered,
                    closure.CreatedAt
                })
            .ToListAsync(cancellationToken);

        var latestClosureByEvent = closureLinks
            .GroupBy(x => x.BzaEventId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var latest = g.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).First();
                    return new ClosureStatusSnapshot(latest.Id, latest.Status, latest.InDeliveryProcess, latest.Delivered);
                });

        var mapped = rawSales
            .Select(s =>
            {
                var closure = latestClosureByEvent.TryGetValue(s.Id, out var resolvedClosure)
                    ? resolvedClosure
                    : null;

                var resolvedStatus = ResolveEventStatus(s.Status, closure);

                return new BzaSaleListDto
                {
                    Id = s.Id,
                    Description = s.Description,
                    PaymentDeadline = s.PaymentDeadline,
                    Status = resolvedStatus,
                    StatusName = StatusNames.GetValueOrDefault(resolvedStatus, "Desconocido"),
                    ClosureEventId = closure?.Id,
                    TotalEventSales = s.TotalEventSales,
                    UnsentSalesAmount = s.UnsentSalesAmount,
                    HasSentSales = s.HasSentSales,
                    UniqueCustomersCount = s.UniqueCustomersCount,
                    TotalCustomers = s.UniqueCustomersCount,
                    TotalAmount = s.TotalEventSales,
                    TotalPaid = s.TotalPaid,
                    TotalPending = Math.Max(0m, s.TotalEventSales - s.TotalPaid),
                    CreatedAt = s.CreatedAt
                };
            });

        if (request.Status.HasValue)
        {
            mapped = mapped.Where(s => s.Status == request.Status.Value);
        }

        return mapped.ToList();
    }

    private sealed record ClosureStatusSnapshot(int Id, int Status, bool InDeliveryProcess, bool Delivered);

    private static int ResolveEventStatus(int currentStatus, ClosureStatusSnapshot? closure)
    {
        if (closure is null)
        {
            return currentStatus == 6 ? 4 : currentStatus;
        }

        if (closure.Status == BzaClosureEventStatus.Cancelled)
        {
            return 5;
        }

        if (closure.Delivered)
        {
            return 4;
        }

        if (closure.InDeliveryProcess)
        {
            return 3;
        }

        return 2;
    }
}


