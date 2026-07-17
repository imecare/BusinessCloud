using BusinessCloud.Application.Admin.Dtos;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Common.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Admin.Queries.GetSystemSellers;

/// <summary>Lista los comisionistas del SaaS con sus totales de comisiones.</summary>
public record GetSystemSellersQuery(bool? OnlyActive = null)
    : IRequest<IReadOnlyList<SystemSellerDto>>;

public class GetSystemSellersHandler(IIdentityDbContext context)
    : IRequestHandler<GetSystemSellersQuery, IReadOnlyList<SystemSellerDto>>
{
    private readonly IIdentityDbContext _context = context;

    public async Task<IReadOnlyList<SystemSellerDto>> Handle(
        GetSystemSellersQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.SystemSellers.AsNoTracking();
        if (request.OnlyActive == true)
            query = query.Where(s => s.IsActive);

        var sellers = await query.ToListAsync(cancellationToken);

        var commissions = await _context.SellerCommissions
            .AsNoTracking()
            .Select(c => new { c.SystemSellerId, c.TenantId, c.Amount, c.IsPaid })
            .ToListAsync(cancellationToken);

        var result = sellers
            .Select(s =>
            {
                var own = commissions.Where(c => c.SystemSellerId == s.Id).ToList();
                return new SystemSellerDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Email = s.Email,
                    Phone = s.Phone,
                    IsActive = s.IsActive,
                    DefaultInitialAmount = s.DefaultInitialAmount,
                    DefaultMonthlyPercent = s.DefaultMonthlyPercent,
                    SalesCount = own.Select(c => c.TenantId).Distinct().Count(),
                    TotalCommission = own.Sum(c => c.Amount),
                    PaidCommission = own.Where(c => c.IsPaid).Sum(c => c.Amount),
                    PendingCommission = own.Where(c => !c.IsPaid).Sum(c => c.Amount),
                };
            })
            .OrderBy(s => s.Name)
            .ToList();

        return result;
    }
}
