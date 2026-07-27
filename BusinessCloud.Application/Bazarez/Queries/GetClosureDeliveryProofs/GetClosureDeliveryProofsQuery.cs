using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetClosureDeliveryProofs;

/// <summary>
/// Detalle de entrega de un Evento de Cierre: grupos participantes y comprobantes
/// de entrega (firmas/fotos de recibido) ya subidos, para la pantalla de Entregas.
/// </summary>
public record GetClosureDeliveryProofsQuery(int ClosureEventId) : IRequest<ClosureDeliveryProofsDto>;

public record DeliveryProofGroupDto(int? CollectorGroupId, string GroupName, int CustomerCount);

public record DeliveryProofItemDto(int Id, int? CollectorGroupId, string GroupName, string ImageUrl, DateTime UploadedAt);

public class ClosureDeliveryProofsDto
{
    public int ClosureEventId { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool InDeliveryProcess { get; set; }
    public bool Delivered { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public List<DeliveryProofGroupDto> Groups { get; set; } = new();
    public List<DeliveryProofItemDto> Proofs { get; set; } = new();
}

public class GetClosureDeliveryProofsHandler(IBazaresDbContext context)
    : IRequestHandler<GetClosureDeliveryProofsQuery, ClosureDeliveryProofsDto>
{
    private readonly IBazaresDbContext _context = context;

    public async Task<ClosureDeliveryProofsDto> Handle(GetClosureDeliveryProofsQuery request, CancellationToken cancellationToken)
    {
        var ev = await _context.ClosureEvents
            .Include(c => c.DeliveryProofs)
                .ThenInclude(p => p.CollectorGroup)
            .Include(c => c.CustomerTotals)
                .ThenInclude(t => t.Customer)
            .FirstOrDefaultAsync(c => c.Id == request.ClosureEventId, cancellationToken)
            ?? throw new KeyNotFoundException("El evento de cierre no existe.");

        var groups = ev.CustomerTotals
            .Where(t => t.Status != Domain.Bazares.Entities.BzaClosureCustomerTotalStatus.Cancelled)
            .GroupBy(t => t.BzaCollectorGroupId)
            .Select(g => new { GroupId = g.Key, Count = g.Select(t => t.BzaCustomerId).Distinct().Count() })
            .ToList();

        var groupIds = groups.Where(g => g.GroupId.HasValue).Select(g => g.GroupId!.Value).ToList();
        var groupNames = await _context.CollectorGroups
            .IgnoreQueryFilters()
            .Where(g => groupIds.Contains(g.Id))
            .ToDictionaryAsync(g => g.Id, g => g.Description, cancellationToken);

        var groupDtos = groups
            .Select(g => new DeliveryProofGroupDto(
                g.GroupId,
                g.GroupId.HasValue ? (groupNames.TryGetValue(g.GroupId.Value, out var n) ? n : "Grupo") : "Sin grupo",
                g.Count))
            .OrderBy(g => g.GroupName)
            .ToList();

        var proofDtos = ev.DeliveryProofs
            .OrderByDescending(p => p.UploadedAt)
            .Select(p => new DeliveryProofItemDto(
                p.Id,
                p.BzaCollectorGroupId,
                p.BzaCollectorGroupId.HasValue ? (p.CollectorGroup?.Description ?? "Grupo") : "General (todos los grupos)",
                p.ImageUrl,
                p.UploadedAt))
            .ToList();

        return new ClosureDeliveryProofsDto
        {
            ClosureEventId = ev.Id,
            Description = ev.Description,
            InDeliveryProcess = ev.InDeliveryProcess,
            Delivered = ev.Delivered,
            DeliveredAt = ev.DeliveredAt,
            Groups = groupDtos,
            Proofs = proofDtos
        };
    }
}