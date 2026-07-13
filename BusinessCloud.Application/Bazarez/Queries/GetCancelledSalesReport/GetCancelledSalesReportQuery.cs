using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetCancelledSalesReport;

/// <summary>
/// Reporte de ventas canceladas por el bazar, con el motivo y si la cancelación fue
/// responsabilidad del cliente. Permite distinguir cancelaciones atribuibles al cliente
/// de las que no lo son.
/// </summary>
public record GetCancelledSalesReportQuery(DateTime? From = null, DateTime? To = null)
    : IRequest<CancelledSalesReportDto>;

public class CancelledSalesReportDto
{
    public int TotalCancellations { get; set; }
    public int CustomerFaultCount { get; set; }
    public int NotCustomerFaultCount { get; set; }
    public decimal TotalAmount { get; set; }

    /// <summary>Resumen por cliente (solo cancelaciones atribuidas al cliente).</summary>
    public List<CancelledSaleCustomerDto> Customers { get; set; } = new();

    /// <summary>Detalle de cada cancelación.</summary>
    public List<CancelledSaleItemDto> Cancellations { get; set; } = new();
}

public record CancelledSaleCustomerDto(
    int CustomerId,
    string CustomerName,
    string? CustomerPhone,
    int CustomerFaultCount,
    DateTime LastCancelledAt);

public record CancelledSaleItemDto(
    int Id,
    int CustomerId,
    string CustomerName,
    string? CustomerPhone,
    string? EventDescription,
    decimal TotalAmount,
    string Reason,
    bool IsCustomerFault,
    DateTime CancelledAt,
    List<string> ProofUrls);

public class GetCancelledSalesReportHandler(IBazaresDbContext context)
    : IRequestHandler<GetCancelledSalesReportQuery, CancelledSalesReportDto>
{
    private readonly IBazaresDbContext _context = context;

    public async Task<CancelledSalesReportDto> Handle(GetCancelledSalesReportQuery request, CancellationToken cancellationToken)
    {
        var query = _context.SaleCancellations.AsQueryable();

        if (request.From.HasValue)
            query = query.Where(c => c.CancelledAt >= request.From.Value);
        if (request.To.HasValue)
            query = query.Where(c => c.CancelledAt <= request.To.Value);

        var cancellations = await query
            .OrderByDescending(c => c.CancelledAt)
            .ToListAsync(cancellationToken);

        var items = cancellations.Select(c => new CancelledSaleItemDto(
            c.Id,
            c.BzaCustomerId,
            c.CustomerName,
            c.CustomerPhone,
            c.EventDescription,
            c.TotalAmount,
            c.Reason,
            c.IsCustomerFault,
            c.CancelledAt,
            string.IsNullOrWhiteSpace(c.ProofUrls)
                ? new List<string>()
                : c.ProofUrls.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()))
            .ToList();

        // El resumen por cliente solo considera cancelaciones atribuidas al cliente,
        // para no penalizar a quienes no tuvieron la culpa.
        var customers = cancellations
            .Where(c => c.IsCustomerFault)
            .GroupBy(c => c.BzaCustomerId)
            .Select(g => new CancelledSaleCustomerDto(
                g.Key,
                g.First().CustomerName,
                g.First().CustomerPhone,
                g.Count(),
                g.Max(c => c.CancelledAt)))
            .OrderByDescending(c => c.CustomerFaultCount)
            .ThenByDescending(c => c.LastCancelledAt)
            .ToList();

        return new CancelledSalesReportDto
        {
            TotalCancellations = cancellations.Count,
            CustomerFaultCount = cancellations.Count(c => c.IsCustomerFault),
            NotCustomerFaultCount = cancellations.Count(c => !c.IsCustomerFault),
            TotalAmount = cancellations.Sum(c => c.TotalAmount),
            Customers = customers,
            Cancellations = items
        };
    }
}
