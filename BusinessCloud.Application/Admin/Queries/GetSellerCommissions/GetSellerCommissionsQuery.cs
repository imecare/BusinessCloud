using BusinessCloud.Application.Admin.Dtos;
using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Admin.Queries.GetSellerCommissions;

/// <summary>Lista las comisiones de un comisionista (opcionalmente solo las no pagadas).</summary>
public record GetSellerCommissionsQuery(int SystemSellerId, bool OnlyUnpaid = false)
    : IRequest<IReadOnlyList<SellerCommissionDto>>;

public class GetSellerCommissionsHandler(IIdentityDbContext context)
    : IRequestHandler<GetSellerCommissionsQuery, IReadOnlyList<SellerCommissionDto>>
{
    private readonly IIdentityDbContext _context = context;

    public async Task<IReadOnlyList<SellerCommissionDto>> Handle(
        GetSellerCommissionsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.SellerCommissions
            .AsNoTracking()
            .Where(c => c.SystemSellerId == request.SystemSellerId);

        if (request.OnlyUnpaid)
            query = query.Where(c => !c.IsPaid);

        var commissions = await query
            .OrderByDescending(c => c.PeriodDate)
            .ToListAsync(cancellationToken);

        var tenantNames = await _context.Tenants
            .AsNoTracking()
            .ToDictionaryAsync(t => t.Id, t => t.Name, cancellationToken);

        return commissions
            .Select(c => new SellerCommissionDto
            {
                Id = c.Id,
                SystemSellerId = c.SystemSellerId,
                TenantId = c.TenantId,
                CompanyName = tenantNames.TryGetValue(c.TenantId, out var name) ? name : c.TenantId,
                Type = c.Type.ToString(),
                BaseAmount = c.BaseAmount,
                Percent = c.Percent,
                Amount = c.Amount,
                PeriodDate = c.PeriodDate,
                IsPaid = c.IsPaid,
                PaidAt = c.PaidAt,
                Notes = c.Notes,
            })
            .ToList();
    }
}
