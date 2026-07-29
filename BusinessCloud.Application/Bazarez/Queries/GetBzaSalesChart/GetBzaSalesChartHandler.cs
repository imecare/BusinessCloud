using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetBzaSalesChart;

public class GetBzaSalesChartHandler(IBazaresDbContext context)
    : IRequestHandler<GetBzaSalesChartQuery, BzaSalesChartDto>
{
    private readonly IBazaresDbContext _context = context;

    private static readonly string[] MonthLabels =
        { "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" };

    public async Task<BzaSalesChartDto> Handle(GetBzaSalesChartQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var year = request.Year ?? now.Year;
        var month = request.Month is >= 1 and <= 12 ? request.Month.Value : now.Month;

        // Monto por venta = suma de precios de sus productos, proyectado en SQL.
        var yearSales = await _context.Sales
            .AsNoTracking()
            .Where(s => s.CreatedAt.Year == year)
            .Select(s => new { s.CreatedAt, Amount = s.Products.Sum(p => (decimal?)p.Price) ?? 0m })
            .ToListAsync(ct);

        // Ventas por mes (12 meses del anio).
        var monthly = new List<SalesBucketDto>();
        for (var m = 1; m <= 12; m++)
        {
            var amount = yearSales.Where(x => x.CreatedAt.Month == m).Sum(x => x.Amount);
            monthly.Add(new SalesBucketDto(MonthLabels[m - 1], amount, m));
        }

        // Ventas por semana del mes seleccionado (bloques de 7 dias).
        var monthSales = yearSales.Where(x => x.CreatedAt.Month == month).ToList();
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var weekCount = (int)Math.Ceiling(daysInMonth / 7.0);
        var weekly = new List<SalesBucketDto>();
        for (var w = 1; w <= weekCount; w++)
        {
            var startDay = (w - 1) * 7 + 1;
            var endDay = Math.Min(w * 7, daysInMonth);
            var amount = monthSales.Where(x => x.CreatedAt.Day >= startDay && x.CreatedAt.Day <= endDay).Sum(x => x.Amount);
            weekly.Add(new SalesBucketDto($"Sem {w}", amount, w));
        }

        // Anios con ventas registradas (para el selector).
        var availableYears = await _context.Sales
            .AsNoTracking()
            .Select(s => s.CreatedAt.Year)
            .Distinct()
            .ToListAsync(ct);
        if (!availableYears.Contains(now.Year)) availableYears.Add(now.Year);
        if (!availableYears.Contains(year)) availableYears.Add(year);
        availableYears = availableYears.OrderByDescending(y => y).ToList();

        return new BzaSalesChartDto(year, month, weekly, monthly, availableYears);
    }
}
