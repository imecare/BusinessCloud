using BusinessCloud.Application.Bazares.Common;
using BusinessCloud.Application.Bazares.Queries.GetClosureEventDetail;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazarez.Queries.SearchClosureCustomerTotals;

/// <summary>
/// Busca los totales de un cliente (por nombre o teléfono) a través de TODOS los eventos
/// de cierre del bazar y los devuelve agrupados por cierre. Permite validar los pagos de
/// un cliente sin tener que abrir cada cierre por separado.
/// </summary>
public record SearchClosureCustomerTotalsQuery(string Query) : IRequest<List<ClosureCustomerSearchGroupDto>>;

/// <summary>
/// Grupo de resultados de la búsqueda: la cabecera del cierre y los totales del cliente
/// que coinciden dentro de ese cierre (misma forma que el detalle para reusar la UI).
/// </summary>
public class ClosureCustomerSearchGroupDto
{
    public int ClosureEventId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Status { get; set; }
    public DateTime? OfficialDeliveryDate { get; set; }
    public DateTime PaymentDeadline { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ClosureCustomerTotalDto> Customers { get; set; } = new();
}

public class SearchClosureCustomerTotalsHandler(IBazaresDbContext context)
    : IRequestHandler<SearchClosureCustomerTotalsQuery, List<ClosureCustomerSearchGroupDto>>
{
    private readonly IBazaresDbContext _context = context;

    public async Task<List<ClosureCustomerSearchGroupDto>> Handle(
        SearchClosureCustomerTotalsQuery request,
        CancellationToken cancellationToken)
    {
        var q = (request.Query ?? string.Empty).Trim();
        if (q.Length == 0)
            return new List<ClosureCustomerSearchGroupDto>();

        var qLower = q.ToLowerInvariant();
        var qDigits = new string(q.Where(char.IsDigit).ToArray());

        bool Matches(BzaClosureCustomerTotal t)
        {
            var customer = t.Customer;
            if (customer == null)
                return false;

            if (!string.IsNullOrEmpty(customer.Name)
                && customer.Name.ToLowerInvariant().Contains(qLower))
                return true;

            if (qDigits.Length > 0 && !string.IsNullOrEmpty(customer.Phone))
            {
                var phoneDigits = new string(customer.Phone.Where(char.IsDigit).ToArray());
                if (phoneDigits.Contains(qDigits))
                    return true;
            }

            return false;
        }

        // Prefiltro traducible a SQL: cierres con al menos un total cuyo cliente coincide
        // por nombre (siempre) o por teléfono (mejor esfuerzo sobre el valor almacenado).
        var closures = await _context.ClosureEvents
            .Include(c => c.CustomerTotals).ThenInclude(t => t.Customer)
            .Include(c => c.CustomerTotals).ThenInclude(t => t.Proofs)
            .Include(c => c.CustomerTotals).ThenInclude(t => t.PackedOrderPhotos)
            .Include(c => c.GroupDeliveries)
            .Where(c => c.CustomerTotals.Any(t => t.Customer != null
                && ((t.Customer.Name != null && t.Customer.Name.ToLower().Contains(qLower))
                    || (qDigits.Length > 0 && t.Customer.Phone != null && t.Customer.Phone.Contains(qDigits)))))
            .ToListAsync(cancellationToken);

        if (closures.Count == 0)
            return new List<ClosureCustomerSearchGroupDto>();

        var bazarSettings = await _context.BazarSettings.FirstOrDefaultAsync(cancellationToken);
        var bazarName = bazarSettings?.BazarName;
        var salesWhatsApp = bazarSettings?.SalesWhatsApp;

        // Nombres de grupo de todos los totales que coinciden en los cierres encontrados.
        var groupIds = closures
            .SelectMany(c => c.CustomerTotals)
            .Where(t => t.BzaCollectorGroupId.HasValue && Matches(t))
            .Select(t => t.BzaCollectorGroupId!.Value)
            .Distinct()
            .ToList();

        var groupNames = await _context.CollectorGroups
            .Where(g => groupIds.Contains(g.Id))
            .ToDictionaryAsync(g => g.Id, g => g.Description, cancellationToken);

        var groups = new List<ClosureCustomerSearchGroupDto>();

        foreach (var closure in closures)
        {
            var matching = closure.CustomerTotals.Where(Matches).ToList();
            if (matching.Count == 0)
                continue;

            var deliveryByGroup = closure.GroupDeliveries
                .GroupBy(g => g.BzaCollectorGroupId)
                .ToDictionary(g => g.Key, g => g.First().DeliveryDate);

            string BuildMessageFor(BzaClosureCustomerTotal t)
            {
                var customerName = t.Customer?.Name ?? "Cliente";
                DateTime? deliveryDate = t.BzaCollectorGroupId.HasValue
                    && deliveryByGroup.TryGetValue(t.BzaCollectorGroupId.Value, out var d)
                        ? d
                        : closure.OfficialDeliveryDate;

                return ClosureMessageBuilder.Build(
                    bazarName, customerName, t.TotalAmount, deliveryDate, closure.PaymentDeadline, salesWhatsApp);
            }

            var customers = matching
                .Select(t => new ClosureCustomerTotalDto(
                    t.Id,
                    t.BzaCustomerId,
                    t.Customer != null ? t.Customer.Name : "Cliente",
                    t.Customer != null ? new string((t.Customer.Phone ?? string.Empty).Where(char.IsDigit).ToArray()) : string.Empty,
                    t.Customer?.HasNoWhatsApp == true,
                    t.Customer != null ? t.Customer.FacebookName : null,
                    t.BzaCollectorGroupId.HasValue && groupNames.TryGetValue(t.BzaCollectorGroupId.Value, out var gn) ? gn : null,
                    t.TotalAmount,
                    t.Status,
                    t.ProofImageUrl,
                    t.ProofUploadedAt,
                    t.UploadToken,
                    t.RejectionReason,
                    t.CustomerJustification,
                    t.Resubmitted,
                    BuildMessageFor(t),
                    t.Proofs
                        .OrderBy(p => p.UploadedAt)
                        .Select(p => new ClosureProofDto(p.Id, p.ImageUrl, p.UploadedAt))
                        .ToList(),
                    t.CancellationReason,
                    t.CancelledIsCustomerFault,
                    t.PaymentMethod,
                    t.CustomerReference,
                    t.ProofUploadedByBazar,
                    t.ValidatedWithoutProof,
                    t.ValidationNote,
                    t.PackedOrderPhotos
                        .OrderBy(p => p.UploadedAt)
                        .ThenBy(p => p.Id)
                        .Select(p => new PackedOrderPhotoDto(p.Id, p.ImageUrl, p.UploadedAt))
                        .ToList()))
                .OrderBy(c => c.CustomerName)
                .ToList();

            groups.Add(new ClosureCustomerSearchGroupDto
            {
                ClosureEventId = closure.Id,
                Description = closure.Description,
                Status = closure.Status,
                OfficialDeliveryDate = closure.OfficialDeliveryDate,
                PaymentDeadline = closure.PaymentDeadline,
                CreatedAt = closure.CreatedAt,
                Customers = customers,
            });
        }

        return groups
            .OrderByDescending(g => g.CreatedAt)
            .ToList();
    }
}
