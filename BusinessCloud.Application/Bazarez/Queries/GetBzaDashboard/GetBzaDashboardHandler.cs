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
        var sales = await _context.Events
            .Include(s => s.Sales).ThenInclude(x => x.Customer).ThenInclude(c => c.Collector).ThenInclude(c => c.CollectorGroup)
            .Include(s => s.Sales).ThenInclude(x => x.Products)
            .Include(s => s.Payments)
            .Where(s => s.CreatedAt >= weekStart)
            .ToListAsync(ct);

        var allPendingSales = await _context.Events
            .Include(s => s.Sales).ThenInclude(x => x.Customer)
            .Include(s => s.Sales).ThenInclude(x => x.Products)
            .Include(s => s.Payments)
            .Where(s => s.Status == 1 && s.PaymentDeadline < today)
            .ToListAsync(ct);

        // Volumen por recolector - agrupando a través de las ventas y sus productos
        var collectorVolume = sales
            .Where(s => s.Status != 5)
            .SelectMany(s => s.Sales, (evt, sale) => new { evt, sale })
            .GroupBy(x => new
            {
                CollectorId = x.sale.Customer.BzaCollectorId,
                x.sale.Customer.Collector.Name,
                x.sale.Customer.Collector.BzaCollectorGroupId,
                GroupDescription = x.sale.Customer.Collector.CollectorGroup != null
                    ? x.sale.Customer.Collector.CollectorGroup.Description
                    : null
            })
            .Select(g =>
            {
                var groupSales = g.ToList();
                var totalSales = groupSales.SelectMany(x => x.sale.Products).Sum(p => p.Price);
                var totalCollected = sales
                    .SelectMany(s => s.Payments)
                    .Where(p => p.IsVerified && groupSales.Select(x => x.sale.BzaCustomerId).Contains(p.BzaCustomerId))
                    .Sum(p => p.Amount);
                return new CollectorVolumeDto
                {
                    CollectorId = g.Key.CollectorId,
                    CollectorName = g.Key.Name,
                    BzaCollectorGroupId = g.Key.BzaCollectorGroupId,
                    GroupDescription = g.Key.GroupDescription,
                    CustomerCount = groupSales.Select(x => x.sale.BzaCustomerId).Distinct().Count(),
                    TotalSales = totalSales,
                    TotalCollected = totalCollected
                };
            })
            .ToList();

        // Morosos - agrupando a través de las ventas y sus productos
        var delinquents = allPendingSales
            .SelectMany(s => s.Sales, (evt, sale) => new { evt, sale })
            .GroupBy(x => new { x.sale.BzaCustomerId, x.sale.Customer.Name, Phone = x.sale.Customer.Phone ?? string.Empty })
            .Select(g =>
            {
                var customerTotal = g.SelectMany(x => x.sale.Products).Sum(p => p.Price);
                var customerPaid = allPendingSales
                    .SelectMany(s => s.Payments)
                    .Where(p => p.IsVerified && p.BzaCustomerId == g.Key.BzaCustomerId)
                    .Sum(p => p.Amount);

                return new DelinquentCustomerDto
                {
                    CustomerId = g.Key.BzaCustomerId,
                    CustomerName = g.Key.Name,
                    CustomerPhone = g.Key.Phone,
                    Balance = Math.Max(0, customerTotal - customerPaid),
                    PaymentDeadline = g.Select(x => x.evt.PaymentDeadline).Min() as DateTime?,
                    OverdueSales = g.Select(x => x.sale.Id).Distinct().Count()
                };
            })
            .Where(d => d.Balance > 0)
            .OrderByDescending(d => d.Balance)
            .ToList();

        var weeklyCollected = sales.SelectMany(s => s.Payments).Where(p => p.IsVerified).Sum(p => p.Amount);
        var weeklySales = sales.Where(s => s.Status != 5).SelectMany(s => s.Sales).SelectMany(x => x.Products).Sum(p => p.Price);

        return new BzaDashboardDto
        {
            TotalCustomers = totalCustomers,
            TotalCollectors = totalCollectors,
            WeeklySales = weeklySales,
            TotalPaid = weeklyCollected,
            TotalPending = weeklySales - weeklyCollected,
            PendingSales = sales.Count(s => s.Status == 1),
            PaidSales = sales.Count(s => s.Status == 2),
            DeliveredSales = sales.Count(s => s.Status == 4),
            DelinquentsCount = delinquents.Count,
            CollectorVolumes = collectorVolume,
            Delinquents = delinquents
        };
    }
}
