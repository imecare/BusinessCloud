using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Bazares.Common;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetClosureEventDetail;

/// <summary>
/// Detalle de un Evento de Cierre de Venta (Envío de Totales): cabecera + totales por
/// cliente con su comprobante, para revisar y validar los pagos.
/// </summary>
public record GetClosureEventDetailQuery(int ClosureEventId) : IRequest<ClosureEventDetailDto>;

public class ClosureEventDetailDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime? OfficialDeliveryDate { get; set; }
    public DateTime PaymentDeadline { get; set; }
    public int Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> EventNames { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public List<ClosureCustomerTotalDto> Customers { get; set; } = new();
}

public record ClosureCustomerTotalDto(
    int Id,
    int CustomerId,
    string CustomerName,
    string CustomerPhone,
    string? GroupName,
    decimal TotalAmount,
    int Status,
    string? ProofImageUrl,
    DateTime? ProofUploadedAt,
    string UploadToken,
    string? RejectionReason,
    string? CustomerJustification,
    bool Resubmitted,
    string Message,
    List<ClosureProofDto> Proofs,
    string? CancellationReason,
    bool? CancelledIsCustomerFault,
    int PaymentMethod,
    string? CustomerReference,
    bool ProofUploadedByBazar,
    bool ValidatedWithoutProof,
    string? ValidationNote);

public class GetClosureEventDetailHandler(IBazaresDbContext context)
    : IRequestHandler<GetClosureEventDetailQuery, ClosureEventDetailDto>
{
    private readonly IBazaresDbContext _context = context;

    public async Task<ClosureEventDetailDto> Handle(GetClosureEventDetailQuery request, CancellationToken cancellationToken)
    {
        var closure = await _context.ClosureEvents
            .Include(c => c.Items)
                .ThenInclude(i => i.Event)
            .Include(c => c.GroupDeliveries)
            .Include(c => c.CustomerTotals)
                .ThenInclude(t => t.Customer)
            .Include(c => c.CustomerTotals)
                .ThenInclude(t => t.Proofs)
            .FirstOrDefaultAsync(c => c.Id == request.ClosureEventId, cancellationToken)
            ?? throw new KeyNotFoundException("El evento de pago no existe.");

        // Nombres de grupo para los totales que tienen grupo asignado.
        var groupIds = closure.CustomerTotals
            .Where(t => t.BzaCollectorGroupId.HasValue)
            .Select(t => t.BzaCollectorGroupId!.Value)
            .Distinct()
            .ToList();

        var groupNames = await _context.CollectorGroups
            .Where(g => groupIds.Contains(g.Id))
            .ToDictionaryAsync(g => g.Id, g => g.Description, cancellationToken);

        var bazarSettings = await _context.BazarSettings.FirstOrDefaultAsync(cancellationToken);
        var bazarName = bazarSettings?.BazarName;

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
                bazarName, customerName, t.TotalAmount, deliveryDate, closure.PaymentDeadline);
        }

        var customers = closure.CustomerTotals
            .Select(t => new ClosureCustomerTotalDto(
                t.Id,
                t.BzaCustomerId,
                t.Customer != null ? t.Customer.Name : "Cliente",
                t.Customer != null ? new string((t.Customer.Phone ?? string.Empty).Where(char.IsDigit).ToArray()) : string.Empty,
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
                t.ValidationNote))
            .OrderBy(c => c.CustomerName)
            .ToList();

        return new ClosureEventDetailDto
        {
            Id = closure.Id,
            Description = closure.Description,
            OfficialDeliveryDate = closure.OfficialDeliveryDate,
            PaymentDeadline = closure.PaymentDeadline,
            Status = closure.Status,
            CreatedAt = closure.CreatedAt,
            EventNames = closure.Items
                .Where(i => i.Event != null)
                .Select(i => i.Event.Description)
                .ToList(),
            TotalAmount = closure.CustomerTotals.Sum(t => t.TotalAmount),
            Customers = customers
        };
    }
}
