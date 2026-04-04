using MediatR;
using BusinessCloud.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Commissions.Queries.GetPendingCommissions;

public class GetPendingCommissionsHandler : IRequestHandler<GetPendingCommissionsQuery, List<PendingCommissionDto>>
{
    private readonly IPaymentsDbContext _context;

    public GetPendingCommissionsHandler(IPaymentsDbContext context) => _context = context;

    public async Task<List<PendingCommissionDto>> Handle(GetPendingCommissionsQuery request, CancellationToken ct)
    {
      // El TenantId se filtra automáticamente por el Global Filter del DbContext [cite: 44]
        return await _context.Sales
            .Where(s => s.SellerId == request.SellerId && !s.IsCommissionPaid)
            .Select(s => new PendingCommissionDto(
                s.Id,
                s.CommissionAmount,
                s.Date
            ))
            .ToListAsync(ct);
    }
}