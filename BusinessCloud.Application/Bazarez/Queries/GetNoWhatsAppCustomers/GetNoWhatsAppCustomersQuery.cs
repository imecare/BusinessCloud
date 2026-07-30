using MediatR;
using Microsoft.EntityFrameworkCore;
using BusinessCloud.Application.Common.Interfaces;

namespace BusinessCloud.Application.Bazares.Queries.GetNoWhatsAppCustomers;

/// <summary>Cliente sin número de WhatsApp (marcado), para el reporte de seguimiento.</summary>
public record NoWhatsAppCustomerDto(
    int Id,
    string Name,
    string PlaceholderNumber,
    string? FacebookName,
    string CollectorName,
    int BzaCollectorId,
    int Status,
    DateTime CreatedAt);

public class NoWhatsAppCustomersReportDto
{
    public int Total { get; set; }
    public List<NoWhatsAppCustomerDto> Customers { get; set; } = [];
}

/// <summary>
/// Reporte de clientes marcados como "sin número de WhatsApp": tienen un placeholder
/// asignado en lugar de un teléfono real. Sirve para dar seguimiento y capturar su
/// número cuando lo proporcionen.
/// </summary>
public record GetNoWhatsAppCustomersQuery : IRequest<NoWhatsAppCustomersReportDto>;

public class GetNoWhatsAppCustomersHandler(IBazaresDbContext context)
    : IRequestHandler<GetNoWhatsAppCustomersQuery, NoWhatsAppCustomersReportDto>
{
    private readonly IBazaresDbContext _context = context;

    public async Task<NoWhatsAppCustomersReportDto> Handle(GetNoWhatsAppCustomersQuery request, CancellationToken ct)
    {
        var customers = await _context.Customers
            .Where(c => c.HasNoWhatsApp)
            .Include(c => c.Collector)
            .OrderBy(c => c.Name)
            .Select(c => new NoWhatsAppCustomerDto(
                c.Id,
                c.Name,
                c.Phone,
                c.FacebookName,
                c.Collector != null ? c.Collector.Name : string.Empty,
                c.BzaCollectorId,
                c.Status,
                c.CreatedAt))
            .ToListAsync(ct);

        return new NoWhatsAppCustomersReportDto
        {
            Total = customers.Count,
            Customers = customers,
        };
    }
}