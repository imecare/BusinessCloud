using MediatR;
using Microsoft.EntityFrameworkCore;
using BusinessCloud.Application.Common.Interfaces;

namespace BusinessCloud.Application.Bazares.Queries.GetMergeCandidates;

/// <summary>
/// Detalle de un cliente candidato a fusión, con el conteo de su historial
/// (ventas y pagos) para ayudar a decidir cuál registro conservar.
/// </summary>
public record MergeCandidateDto(
    int Id,
    string Name,
    string Phone,
    string? FacebookName,
    int Status,
    int BzaCollectorId,
    string? CollectorName,
    int SalesCount,
    int PaymentsCount);

/// <summary>
/// Obtiene el detalle completo de los clientes seleccionados para fusionar,
/// incluyendo recolector e historial, a partir de sus IDs.
/// </summary>
public record GetMergeCandidatesQuery(IReadOnlyList<int> CustomerIds) : IRequest<List<MergeCandidateDto>>;

public class GetMergeCandidatesHandler : IRequestHandler<GetMergeCandidatesQuery, List<MergeCandidateDto>>
{
    private readonly IBazaresDbContext _context;

    public GetMergeCandidatesHandler(IBazaresDbContext context) => _context = context;

    public async Task<List<MergeCandidateDto>> Handle(GetMergeCandidatesQuery request, CancellationToken ct)
    {
        var ids = (request.CustomerIds ?? []).Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        var customers = await _context.Customers
            .Include(c => c.Collector)
            .Where(c => ids.Contains(c.Id))
            .ToListAsync(ct);

        // Conteo de ventas por cliente (historial de compras).
        var salesCounts = await _context.Sales
            .Where(s => ids.Contains(s.BzaCustomerId))
            .GroupBy(s => s.BzaCustomerId)
            .Select(g => new { CustomerId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        // Conteo de pagos por cliente.
        var paymentCounts = await _context.Payments
            .Where(p => ids.Contains(p.BzaCustomerId))
            .GroupBy(p => p.BzaCustomerId)
            .Select(g => new { CustomerId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var salesMap = salesCounts.ToDictionary(x => x.CustomerId, x => x.Count);
        var paymentsMap = paymentCounts.ToDictionary(x => x.CustomerId, x => x.Count);

        // Se respeta el orden en que el usuario seleccionó los clientes.
        return ids
            .Select(id => customers.FirstOrDefault(c => c.Id == id))
            .Where(c => c is not null)
            .Select(c => new MergeCandidateDto(
                c!.Id,
                c.Name,
                c.Phone,
                c.FacebookName,
                c.Status,
                c.BzaCollectorId,
                c.Collector != null ? c.Collector.Name : null,
                salesMap.GetValueOrDefault(c.Id, 0),
                paymentsMap.GetValueOrDefault(c.Id, 0)))
            .ToList();
    }
}
