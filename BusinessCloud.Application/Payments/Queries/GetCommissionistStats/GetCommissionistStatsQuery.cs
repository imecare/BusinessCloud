using BusinessCloud.Application.Payments.Dtos;
using MediatR;

namespace BusinessCloud.Application.Payments.Queries.GetCommissionistStats;

public record GetCommissionistStatsQuery : IRequest<CommissionistStatsDto>;