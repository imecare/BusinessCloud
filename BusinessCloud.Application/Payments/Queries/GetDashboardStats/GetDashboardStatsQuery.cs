using MediatR;

namespace BusinessCloud.Application.Payments.Queries.GetDashboardStats;

public record GetDashboardStatsQuery : IRequest<DashboardStatsDto>;
