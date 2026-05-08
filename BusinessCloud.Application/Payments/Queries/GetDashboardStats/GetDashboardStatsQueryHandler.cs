using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Queries.GetDashboardStats;

public class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    private readonly IPaymentsDbContext _db;

    public GetDashboardStatsQueryHandler(IPaymentsDbContext db) => _db = db;

    public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        var salesData = await _db.Sales
            .AsNoTracking()
            .Include(s => s.Payment)
            .Select(s => new
            {
                s.TotalAmount,
                s.CostPrice,
                s.CommissionAmount,
                s.IsCommissionPaid,
                Collected = s.Payment.Where(p => p.PaymentTypeId == 2).Sum(p => p.Amount)
            })
            .ToListAsync(cancellationToken);

        var totalSales = salesData.Sum(x => x.TotalAmount);
        var totalCollected = salesData.Sum(x => x.Collected);
        var totalCost = salesData.Sum(x => x.CostPrice);

        var activeCustomers = await _db.Customers.CountAsync(cancellationToken);
        var activeSellers = await _db.Sellers.CountAsync(cancellationToken);

        return new DashboardStatsDto
        {
            TotalSales = totalSales,
            TotalCollected = totalCollected,
            PendingCollection = totalSales - totalCollected,
            PendingCommissions = salesData.Where(x => !x.IsCommissionPaid && x.CommissionAmount > 0).Sum(x => x.CommissionAmount),
            PaidCommissions = salesData.Where(x => x.IsCommissionPaid).Sum(x => x.CommissionAmount),
            ActiveCustomers = activeCustomers,
            ActiveSellers = activeSellers,
            TotalProfit = totalSales - totalCost
        };
    }
}
