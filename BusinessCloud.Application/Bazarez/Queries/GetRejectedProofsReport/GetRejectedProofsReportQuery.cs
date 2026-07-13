using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetRejectedProofsReport;

/// <summary>
/// Reporte de comprobantes rechazados: clientes a los que se les ha rechazado
/// algún comprobante, con los motivos y las referencias de los comprobantes.
/// Permite detectar clientes que reiteran comprobantes inválidos.
/// </summary>
public record GetRejectedProofsReportQuery(DateTime? From = null, DateTime? To = null)
    : IRequest<RejectedProofsReportDto>;

public class RejectedProofsReportDto
{
    public int TotalRejections { get; set; }
    public int CustomersAffected { get; set; }

    /// <summary>Resumen por cliente (para detectar reincidencias).</summary>
    public List<RejectedProofCustomerDto> Customers { get; set; } = new();

    /// <summary>Detalle de cada rechazo.</summary>
    public List<RejectedProofItemDto> Rejections { get; set; } = new();
}

public record RejectedProofCustomerDto(
    int CustomerId,
    string CustomerName,
    string? CustomerPhone,
    int RejectionCount,
    DateTime LastRejectedAt);

public record RejectedProofItemDto(
    int Id,
    int CustomerId,
    string CustomerName,
    string? CustomerPhone,
    string? EventDescription,
    decimal TotalAmount,
    string Reason,
    DateTime RejectedAt,
    List<string> ProofUrls);

public class GetRejectedProofsReportHandler(IBazaresDbContext context)
    : IRequestHandler<GetRejectedProofsReportQuery, RejectedProofsReportDto>
{
    private readonly IBazaresDbContext _context = context;

    public async Task<RejectedProofsReportDto> Handle(GetRejectedProofsReportQuery request, CancellationToken cancellationToken)
    {
        var query = _context.ProofRejections.AsQueryable();

        if (request.From.HasValue)
            query = query.Where(r => r.RejectedAt >= request.From.Value);
        if (request.To.HasValue)
            query = query.Where(r => r.RejectedAt <= request.To.Value);

        var rejections = await query
            .OrderByDescending(r => r.RejectedAt)
            .ToListAsync(cancellationToken);

        var items = rejections.Select(r => new RejectedProofItemDto(
            r.Id,
            r.BzaCustomerId,
            r.CustomerName,
            r.CustomerPhone,
            r.EventDescription,
            r.TotalAmount,
            r.Reason,
            r.RejectedAt,
            string.IsNullOrWhiteSpace(r.ProofUrls)
                ? new List<string>()
                : r.ProofUrls.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()))
            .ToList();

        var customers = rejections
            .GroupBy(r => r.BzaCustomerId)
            .Select(g => new RejectedProofCustomerDto(
                g.Key,
                g.First().CustomerName,
                g.First().CustomerPhone,
                g.Count(),
                g.Max(r => r.RejectedAt)))
            .OrderByDescending(c => c.RejectionCount)
            .ThenByDescending(c => c.LastRejectedAt)
            .ToList();

        return new RejectedProofsReportDto
        {
            TotalRejections = rejections.Count,
            CustomersAffected = customers.Count,
            Customers = customers,
            Rejections = items
        };
    }
}
