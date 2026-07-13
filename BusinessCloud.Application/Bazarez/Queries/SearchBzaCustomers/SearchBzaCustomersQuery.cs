using MediatR;
using Microsoft.EntityFrameworkCore;
using BusinessCloud.Application.Common.Interfaces;

namespace BusinessCloud.Application.Bazares.Queries.SearchBzaCustomers;

/// <summary>Resultado de búsqueda de clientes por nombre o teléfono.</summary>
public record BzaCustomerSearchDto(
    int Id,
    string Name,
    string Phone,
    string? FacebookName,
    int Status,
    string? CollectorName);

/// <summary>Busca clientes por nombre o número telefónico (llave de envío de totales).</summary>
public record SearchBzaCustomersQuery(string? Query) : IRequest<List<BzaCustomerSearchDto>>;

public class SearchBzaCustomersHandler : IRequestHandler<SearchBzaCustomersQuery, List<BzaCustomerSearchDto>>
{
    private readonly IBazaresDbContext _context;

    public SearchBzaCustomersHandler(IBazaresDbContext context) => _context = context;

    public async Task<List<BzaCustomerSearchDto>> Handle(SearchBzaCustomersQuery request, CancellationToken ct)
    {
        var raw = (request.Query ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        // Si la búsqueda contiene dígitos suficientes, se interpreta como teléfono.
        var digits = new string(raw.Where(char.IsDigit).ToArray());

        var query = _context.Customers
            .Include(c => c.Collector)
            .AsQueryable();

        if (digits.Length >= 3)
        {
            query = query.Where(c => c.Name.Contains(raw) || c.Phone.Contains(digits));
        }
        else
        {
            query = query.Where(c => c.Name.Contains(raw) || c.Phone.Contains(raw));
        }

        return await query
            .OrderBy(c => c.Name)
            .Take(25)
            .Select(c => new BzaCustomerSearchDto(
                c.Id,
                c.Name,
                c.Phone,
                c.FacebookName,
                c.Status,
                c.Collector != null ? c.Collector.Name : null))
            .ToListAsync(ct);
    }
}
