using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetPendingMoveOptions;

/// <summary>
/// Opciones para mover las ventas pendientes de comprobante (aún sin subir) de un
/// evento de cierre antes de marcarlo "en proceso de entrega": cuántas hay y a qué
/// otros eventos de cierre (no cancelados, no procesados) se pueden mover.
/// </summary>
public record GetPendingMoveOptionsQuery(int ClosureEventId)
    : IRequest<PendingMoveOptionsDto>;

public class PendingMoveOptionsDto
{
    public int ClosureEventId { get; set; }
    /// <summary>Clientes con venta pendiente (aún no suben comprobante) en este evento.</summary>
    public int PendingCount { get; set; }
    /// <summary>Otros eventos de cierre válidos como destino (no cancelados, no procesados).</summary>
    public List<PendingMoveCandidateDto> Candidates { get; set; } = new();
}

public record PendingMoveCandidateDto(
    int ClosureEventId,
    string Description,
    DateTime? DeliveryDate,
    DateTime PaymentDeadline);

public class GetPendingMoveOptionsHandler(IBazaresDbContext context)
    : IRequestHandler<GetPendingMoveOptionsQuery, PendingMoveOptionsDto>
{
    private readonly IBazaresDbContext _context = context;

    public async Task<PendingMoveOptionsDto> Handle(GetPendingMoveOptionsQuery request, CancellationToken cancellationToken)
    {
        var closure = await _context.ClosureEvents
            .FirstOrDefaultAsync(c => c.Id == request.ClosureEventId, cancellationToken)
            ?? throw new KeyNotFoundException("El evento de cierre no existe.");

        var pendingCount = await _context.ClosureCustomerTotals
            .CountAsync(t => t.BzaClosureEventId == request.ClosureEventId
                              && t.Status == BzaClosureCustomerTotalStatus.Pending, cancellationToken);

        var today = DateTime.UtcNow.Date;
        var others = await _context.ClosureEvents
            .Where(c => c.Id != request.ClosureEventId
                        && c.Status != BzaClosureEventStatus.Cancelled
                        && !c.InDeliveryProcess
                        && c.OfficialDeliveryDate != null
                        && c.OfficialDeliveryDate.Value.Date >= today)
            .OrderBy(c => c.OfficialDeliveryDate)
            .Select(c => new PendingMoveCandidateDto(c.Id, c.Description, c.OfficialDeliveryDate, c.PaymentDeadline))
            .ToListAsync(cancellationToken);

        return new PendingMoveOptionsDto
        {
            ClosureEventId = closure.Id,
            PendingCount = pendingCount,
            Candidates = others
        };
    }
}