using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetBzaDashboard;

public class GetBzaDashboardHandler(IBazaresDbContext context)
    : IRequestHandler<GetBzaDashboardQuery, BzaDashboardDto>
{
    private readonly IBazaresDbContext _context = context;

    public async Task<BzaDashboardDto> Handle(GetBzaDashboardQuery request, CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);

        var totalCustomers = await _context.Customers.CountAsync(ct);
        var totalCollectors = await _context.Collectors.CountAsync(ct);

        // Obtener eventos de venta de la semana con productos y pagos
        var sales = await _context.Sales
            .Include(s => s.SoldProducts).ThenInclude(p => p.Customer).ThenInclude(c => c.Collector).ThenInclude(c => c.CollectorGroup)
            .Include(s => s.Payments)
            .Where(s => s.CreatedAt >= weekStart)
            .ToListAsync(ct);

        var allPendingSales = await _context.Sales
            .Include(s => s.SoldProducts).ThenInclude(p => p.Customer)
            .Include(s => s.Payments)
            .Where(s => s.Status == 1 && s.PaymentDeadline < today)
            .ToListAsync(ct);

        // Volumen por recolector - agrupando a través de SoldProducts
        var collectorVolume = sales
            .Where(s => s.Status != 5)
            .SelectMany(s => s.SoldProducts, (sale, product) => new { sale, product })
            .GroupBy(x => new
            {
                x.product.Customer.Collector.Name,
                x.product.Customer.Collector.BzaCollectorGroupId,
                GroupDescription = x.product.Customer.Collector.CollectorGroup != null
                    ? x.product.Customer.Collector.CollectorGroup.Description
                    : null
            })
            .Select(g => new CollectorVolumeDto
            {
                CollectorName = g.Key.Name,
                BzaCollectorGroupId = g.Key.BzaCollectorGroupId,
                GroupDescription = g.Key.GroupDescription,
                PackageCount = g.Select(x => x.sale.Id).Distinct().Count(),
                TotalValue = g.Sum(x => x.product.Price)
            })
            .ToList();

        // Morosos - agrupando a través de SoldProducts
        var delinquents = allPendingSales
            .SelectMany(s => s.SoldProducts, (sale, product) => new { sale, product })
            .GroupBy(x => new { x.product.BzaCustomerId, x.product.Customer.Name })
            .Select(g =>
            {
                var customerTotal = g.Sum(x => x.product.Price);
                var customerPaid = allPendingSales
                    .SelectMany(s => s.Payments)
                    .Where(p => p.IsVerified && p.BzaCustomerId == g.Key.BzaCustomerId)
                    .Sum(p => p.Amount);

                return new DelinquentCustomerDto
                {
                    CustomerId = g.Key.BzaCustomerId,
                    CustomerName = g.Key.Name,
                    AmountOwed = Math.Max(0, customerTotal - customerPaid),
                    OldestDeadline = g.Select(x => x.sale.PaymentDeadline).Min(),
                    OverdueSales = g.Select(x => x.sale.Id).Distinct().Count()
                };
            })
            .Where(d => d.AmountOwed > 0)
            .OrderByDescending(d => d.AmountOwed)
            .ToList();

        var weeklyCollected = sales.SelectMany(s => s.Payments).Where(p => p.IsVerified).Sum(p => p.Amount);
        var weeklySales = sales.Where(s => s.Status != 5).SelectMany(s => s.SoldProducts).Sum(p => p.Price);

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
