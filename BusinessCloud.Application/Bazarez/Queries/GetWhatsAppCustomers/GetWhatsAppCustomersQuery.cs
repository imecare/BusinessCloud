using MediatR;
using Microsoft.EntityFrameworkCore;
using BusinessCloud.Application.Common.Interfaces;

namespace BusinessCloud.Application.Bazares.Queries.GetWhatsAppCustomers;

/// <summary>Cliente con número de WhatsApp disponible para contacto.</summary>
public record WhatsAppCustomerDto(
    int Id,
    string Name,
    string InternalNumber,
    string? FacebookName,
    string CollectorName,
    string WhatsAppNumber);

public class WhatsAppCustomersReportDto
{
    public int Total { get; set; }
    public List<WhatsAppCustomerDto> Customers { get; set; } = [];
}

/// <summary>
/// Reporte de clientes que tienen un número de WhatsApp (no marcados como "sin WhatsApp").
/// Devuelve nombre, número interno, recolector, número de WhatsApp y perfil de Facebook.
/// </summary>
public record GetWhatsAppCustomersQuery : IRequest<WhatsAppCustomersReportDto>;

public class GetWhatsAppCustomersHandler(IBazaresDbContext context)
    : IRequestHandler<GetWhatsAppCustomersQuery, WhatsAppCustomersReportDto>
{
    private readonly IBazaresDbContext _context = context;

    public async Task<WhatsAppCustomersReportDto> Handle(GetWhatsAppCustomersQuery request, CancellationToken ct)
    {
        var customers = await _context.Customers
            .Where(c => !c.HasNoWhatsApp && !string.IsNullOrEmpty(c.Phone))
            .Include(c => c.Collector)
            .OrderBy(c => c.Name)
            .Select(c => new WhatsAppCustomerDto(
                c.Id,
                c.Name,
                c.Phone,
                c.FacebookName,
                c.Collector != null ? c.Collector.Name : string.Empty,
                c.Phone))
            .ToListAsync(ct);

        return new WhatsAppCustomersReportDto
        {
            Total = customers.Count,
            Customers = customers,
        };
    }
}
