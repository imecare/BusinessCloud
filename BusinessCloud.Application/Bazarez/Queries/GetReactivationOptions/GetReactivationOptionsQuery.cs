using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetReactivationOptions;

/// <summary>
/// Opciones para reactivar una venta cancelada: indica si es necesario reasignar el
/// evento de pago (porque ya se procesaron etiquetas o ya venció la fecha de entrega)
/// y ofrece los eventos de cierre existentes cuya fecha de entrega aún no ha pasado.
/// </summary>
public record GetReactivationOptionsQuery(int ClosureCustomerTotalId)
    : IRequest<ReactivationOptionsDto>;

public class ReactivationOptionsDto
{
    public int ClosureCustomerTotalId { get; set; }
    public DateTime? CurrentDeliveryDate { get; set; }
    public DateTime CurrentPaymentDeadline { get; set; }
    /// <summary>La fecha de entrega del grupo del cliente ya pasó.</summary>
    public bool DeliveryPassed { get; set; }
    /// <summary>El cierre ya entró en proceso de entrega (etiquetas/despacho impresos).</summary>
    public bool LabelsProcessed { get; set; }
    /// <summary>Se recomienda reasignar a otro evento (DeliveryPassed || LabelsProcessed).</summary>
    public bool NeedsReassign { get; set; }
    /// <summary>Eventos de cierre existentes válidos como destino (entrega futura).</summary>
    public List<ReactivationCandidateDto> Candidates { get; set; } = new();
}

public record ReactivationCandidateDto(
    int ClosureEventId,
    string Description,
    DateTime? DeliveryDate,
    DateTime PaymentDeadline);

public class GetReactivationOptionsHandler(IBazaresDbContext context)
    : IRequestHandler<GetReactivationOptionsQuery, ReactivationOptionsDto>
{
    private readonly IBazaresDbContext _context = context;

    public async Task<ReactivationOptionsDto> Handle(GetReactivationOptionsQuery request, CancellationToken cancellationToken)
    {
        var total = await _context.ClosureCustomerTotals
            .Include(t => t.ClosureEvent)
                .ThenInclude(c => c.GroupDeliveries)
            .FirstOrDefaultAsync(t => t.Id == request.ClosureCustomerTotalId, cancellationToken)
            ?? throw new KeyNotFoundException("El total del cliente no existe.");

        var today = DateTime.UtcNow.Date;
        var groupId = total.BzaCollectorGroupId;

        DateTime? ResolveDelivery(BzaClosureEvent ev)
            => groupId.HasValue
                ? ev.GroupDeliveries.FirstOrDefault(g => g.BzaCollectorGroupId == groupId.Value)?.DeliveryDate
                    ?? ev.OfficialDeliveryDate
                : ev.OfficialDeliveryDate;

        var currentDelivery = ResolveDelivery(total.ClosureEvent);
        var deliveryPassed = currentDelivery.HasValue && currentDelivery.Value.Date < today;
        var labelsProcessed = total.ClosureEvent.InDeliveryProcess;

        // Candidatos: otros cierres no cancelados, sin etiquetas procesadas y cuya
        // fecha de entrega (para el grupo del cliente) aún no haya pasado.
        var others = await _context.ClosureEvents
            .Include(c => c.GroupDeliveries)
            .Where(c => c.Id != total.BzaClosureEventId
                        && c.Status != BzaClosureEventStatus.Cancelled
                        && !c.InDeliveryProcess)
            .ToListAsync(cancellationToken);

        var candidates = others
            .Select(c => new { Closure = c, Delivery = ResolveDelivery(c) })
            .Where(x => x.Delivery.HasValue && x.Delivery.Value.Date >= today)
            .OrderBy(x => x.Delivery!.Value)
            .Select(x => new ReactivationCandidateDto(
                x.Closure.Id,
                x.Closure.Description,
                x.Delivery,
                x.Closure.PaymentDeadline))
            .ToList();

        return new ReactivationOptionsDto
        {
            ClosureCustomerTotalId = total.Id,
            CurrentDeliveryDate = currentDelivery,
            CurrentPaymentDeadline = total.ClosureEvent.PaymentDeadline,
            DeliveryPassed = deliveryPassed,
            LabelsProcessed = labelsProcessed,
            NeedsReassign = deliveryPassed || labelsProcessed,
            Candidates = candidates
        };
    }
}
