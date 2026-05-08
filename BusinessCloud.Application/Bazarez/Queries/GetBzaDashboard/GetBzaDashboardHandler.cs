using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetBzaDashboard;

public class GetBzaDashboardHandler : IRequestHandler<GetBzaDashboardQuery, BzaDashboardDto>
{
    private readonly IBazaresDbContext _context;

    public GetBzaDashboardHandler(IBazaresDbContext context) => _context = context;

    public async Task<BzaDashboardDto> Handle(GetBzaDashboardQuery request, CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);

        var totalCustomers = await _context.Customers.CountAsync(ct);
        var totalCollectors = await _context.Collectors.CountAsync(ct);

        var sales = await _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.Payments)
            .Where(s => s.CreatedAt >= weekStart)
            .ToListAsync(ct);

        var allPendingSales = await _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.Payments)
            .Where(s => s.Status == 1 && s.PaymentDeadline < today)
            .ToListAsync(ct);

        // Volumen por recolector
        var collectorVolume = await _context.Sales
            .Include(s => s.Customer).ThenInclude(c => c.Collector)
            .Where(s => s.CreatedAt >= weekStart && s.Status != 5)
            .GroupBy(s => new { s.Customer.Collector.Name, s.Customer.Collector.GroupId })
            .Select(g => new CollectorVolumeDto
            {
                CollectorName = g.Key.Name,
                GroupId = g.Key.GroupId,
                PackageCount = g.Count(),
                TotalValue = g.Sum(s => s.Total)
            })
            .ToListAsync(ct);

        // Morosos
        var delinquents = allPendingSales
            .GroupBy(s => new { s.BzaCustomerId, s.Customer.Name })
            .Select(g => new DelinquentCustomerDto
            {
                CustomerId = g.Key.BzaCustomerId,
                CustomerName = g.Key.Name,
                AmountOwed = g.Sum(s => s.Total - s.Payments.Where(p => p.IsVerified).Sum(p => p.Amount)),
                OldestDeadline = g.Min(s => s.PaymentDeadline),
                OverdueSales = g.Count()
            })
            .Where(d => d.AmountOwed > 0)
            .OrderByDescending(d => d.AmountOwed)
            .ToList();

        var weeklyCollected = sales.SelectMany(s => s.Payments).Where(p => p.IsVerified).Sum(p => p.Amount);
        var weeklySales = sales.Where(s => s.Status != 5).Sum(s => s.Total);

        return new BzaDashboardDto
        {
            TotalCustomers = totalCustomers,
            TotalCollectors = totalCollectors,
            WeeklySales = weeklySales,
            WeeklyCollected = weeklyCollected,
            PendingCollection = weeklySales - weeklyCollected,
            PendingSales = sales.Count(s => s.Status == 1),
            PaidSales = sales.Count(s => s.Status == 2),
            DeliveredSales = sales.Count(s => s.Status == 4),
            CollectorVolume = collectorVolume,
            Delinquents = delinquents
        };
    }
}
