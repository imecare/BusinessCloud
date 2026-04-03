using MediatR;
namespace BusinessCloud.Application.Commissions.Queries.GetPendingCommissions;

public record GetPendingCommissionsQuery(int SellerId) : IRequest<List<PendingCommissionDto>>;

public record PendingCommissionDto(int SaleId, decimal CommissionAmount, DateTime Date);