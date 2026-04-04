using MediatR;
using BusinessCloud.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Dashboard.Queries.GetFinancialSummary;

public class GetFinancialSummaryHandler : IRequestHandler<GetFinancialSummaryQuery, FinancialSummaryDto>
{
    private readonly IPaymentsDbContext _context;

    public GetFinancialSummaryHandler(IPaymentsDbContext context) => _context = context;

    public async Task<FinancialSummaryDto> Handle(GetFinancialSummaryQuery request, CancellationToken ct)
    {
       // El TenantId se filtra automáticamente por el Global Filter del DbContext 
       // Usar .Select es una práctica Senior para optimizar el rendimiento de la red 
        var salesData = await _context.Sales
            .Where(s => s.Date >= request.StartDate && s.Date <= request.EndDate)
            .Select(s => new { s.TotalAmount, s.CostPrice, s.CommissionAmount })
            .ToListAsync(ct);

        var totalSales = salesData.Sum(x => x.TotalAmount);
        var totalCosts = salesData.Sum(x => x.CostPrice);
        var totalCommissions = salesData.Sum(x => x.CommissionAmount);

        // Retornamos el DTO calculando la Utilidad Real: Ventas - Costo - Comisiones 
        return new FinancialSummaryDto(
            TotalSales: totalSales,
            TotalCosts: totalCosts,
            TotalCommissions: totalCommissions,
            NetProfit: totalSales - totalCosts - totalCommissions
        );
    }
}