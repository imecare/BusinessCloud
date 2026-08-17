using MediatR;

namespace BusinessCloud.Application.Bazares.Queries.GetBzaClosureAnalytics;

public record GetBzaClosureAnalyticsQuery(int? Year = null) : IRequest<BzaClosureAnalyticsDto>;

public record BzaClosureEventMetricDto(
    int ClosureEventId,
    string Description,
    DateTime CreatedAt,
    int ProductCount,
    decimal TotalSales,
    decimal TotalPaid,
    decimal TotalUnpaid);

public record BzaMonthMetricDto(
    int Year,
    int Month,
    string Label,
    int ProductCount,
    decimal TotalSales,
    decimal TotalPaid,
    decimal TotalUnpaid);

public record BzaClosureAnalyticsDto(
    List<BzaClosureEventMetricDto> PerEvent,
    List<BzaMonthMetricDto> PerMonth,
    List<int> AvailableYears);
