using System.Globalization;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetBzaClosureAnalytics;

public class GetBzaClosureAnalyticsHandler(IBazaresDbContext context)
    : IRequestHandler<GetBzaClosureAnalyticsQuery, BzaClosureAnalyticsDto>
{
    private static readonly CultureInfo MexicanSpanish = CultureInfo.GetCultureInfo("es-MX");

    public async Task<BzaClosureAnalyticsDto> Handle(
        GetBzaClosureAnalyticsQuery request,
        CancellationToken cancellationToken)
    {
        var closures = await context.ClosureEvents
            .AsNoTracking()
            .Where(closure => closure.Status != BzaClosureEventStatus.Cancelled)
            .Select(closure => new ClosureRow(
                closure.Id,
                closure.Description,
                closure.CreatedAt))
            .ToListAsync(cancellationToken);

        var totalsByClosure = await context.ClosureCustomerTotals
            .AsNoTracking()
            .Where(total =>
                total.ClosureEvent.Status != BzaClosureEventStatus.Cancelled &&
                total.Status != BzaClosureCustomerTotalStatus.Cancelled)
            .GroupBy(total => total.BzaClosureEventId)
            .Select(group => new TotalRow(
                group.Key,
                group.Sum(total => total.TotalAmount),
                group.Sum(total => total.Status == BzaClosureCustomerTotalStatus.Validated
                    ? total.TotalAmount
                    : 0m)))
            .ToDictionaryAsync(row => row.ClosureEventId, cancellationToken);

        var productsByClosure = await context.SoldProducts
            .AsNoTracking()
            .Where(product => product.Sale.BzaClosureEventId != null)
            .GroupBy(product => product.Sale.BzaClosureEventId!.Value)
            .Select(group => new ProductRow(group.Key, group.Count()))
            .ToDictionaryAsync(row => row.ClosureEventId, cancellationToken);

        var perEvent = closures
            .Select(closure =>
            {
                totalsByClosure.TryGetValue(closure.Id, out var totals);
                productsByClosure.TryGetValue(closure.Id, out var products);
                var totalSales = totals?.TotalSales ?? 0m;
                var totalPaid = totals?.TotalPaid ?? 0m;

                return new BzaClosureEventMetricDto(
                    closure.Id,
                    closure.Description,
                    closure.CreatedAt,
                    products?.ProductCount ?? 0,
                    totalSales,
                    totalPaid,
                    Math.Max(0m, totalSales - totalPaid));
            })
            .OrderByDescending(metric => metric.CreatedAt)
            .ToList();

        var now = DateTime.UtcNow;
        var firstMonth = request.Year.HasValue
            ? new DateTime(request.Year.Value, 1, 1)
            : new DateTime(now.Year, now.Month, 1).AddMonths(-11);

        var perMonth = Enumerable.Range(0, 12)
            .Select(offset => firstMonth.AddMonths(offset))
            .Select(month =>
            {
                var metrics = perEvent.Where(metric =>
                    metric.CreatedAt.Year == month.Year && metric.CreatedAt.Month == month.Month);

                return new BzaMonthMetricDto(
                    month.Year,
                    month.Month,
                    FormatMonthLabel(month),
                    metrics.Sum(metric => metric.ProductCount),
                    metrics.Sum(metric => metric.TotalSales),
                    metrics.Sum(metric => metric.TotalPaid),
                    metrics.Sum(metric => metric.TotalUnpaid));
            })
            .ToList();

        var availableYears = closures
            .Select(closure => closure.CreatedAt.Year)
            .Distinct()
            .OrderByDescending(year => year)
            .ToList();

        return new BzaClosureAnalyticsDto(perEvent, perMonth, availableYears);
    }

    private static string FormatMonthLabel(DateTime month)
    {
        var abbreviatedMonth = month.ToString("MMM", MexicanSpanish).TrimEnd('.');
        return $"{MexicanSpanish.TextInfo.ToTitleCase(abbreviatedMonth)} {month.Year}";
    }

    private sealed record ClosureRow(int Id, string Description, DateTime CreatedAt);
    private sealed record TotalRow(int ClosureEventId, decimal TotalSales, decimal TotalPaid);
    private sealed record ProductRow(int ClosureEventId, int ProductCount);
}
