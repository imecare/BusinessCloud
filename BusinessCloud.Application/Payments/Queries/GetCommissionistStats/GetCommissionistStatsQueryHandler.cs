using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Payments.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Queries.GetCommissionistStats;

public class GetCommissionistStatsQueryHandler : IRequestHandler<GetCommissionistStatsQuery, CommissionistStatsDto>
{
    private readonly IPaymentsDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCommissionistStatsQueryHandler(IPaymentsDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CommissionistStatsDto> Handle(GetCommissionistStatsQuery request, CancellationToken cancellationToken)
    {
        var sellerId = _currentUser.SellerId
            ?? throw new UnauthorizedAccessException("No se pudo determinar el SellerId del token.");

        var sales = await _db.Sales
            .AsNoTracking()
            .Where(s => s.SellerId == sellerId)
            .ToListAsync(cancellationToken);

        var totalCustomers = await _db.Customers
            .AsNoTracking()
            .CountAsync(c => c.SellerId == sellerId, cancellationToken);

        return new CommissionistStatsDto
        {
            TotalCustomers = totalCustomers,
            TotalSales = sales.Count,
            PaidSales = sales.Count(s => s.IsPaid),
            PendingCommissionsAmount = sales.Where(s => !s.IsCommissionPaid && s.CommissionAmount > 0).Sum(s => s.CommissionAmount),
            PaidCommissionsAmount = sales.Where(s => s.IsCommissionPaid).Sum(s => s.CommissionAmount),
            PendingCommissionsCount = sales.Count(s => !s.IsCommissionPaid && s.CommissionAmount > 0),
            PaidCommissionsCount = sales.Count(s => s.IsCommissionPaid)
        };
    }
}