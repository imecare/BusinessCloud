using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.CancelPendingSales;

/// <summary>
/// Cancela por sistema todas las ventas con estatus Pendiente (aún sin comprobante) de
/// un evento de cierre, típicamente al marcarlo "en proceso de entrega" sin haberlas
/// movido a otro evento. Cada una queda registrada como cancelación y puede reactivarse
/// después desde Validación de comprobantes.
/// </summary>
public record CancelPendingSalesCommand(int ClosureEventId, string? Reason = null)
    : IRequest<CancelPendingSalesResultDto>;

public class CancelPendingSalesResultDto
{
    public int ClosureEventId { get; set; }
    public int CancelledCount { get; set; }
}