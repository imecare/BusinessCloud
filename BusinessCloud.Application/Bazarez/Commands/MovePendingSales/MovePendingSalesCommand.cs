using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.MovePendingSales;

/// <summary>Destino al mover las ventas pendientes (sin comprobante) de un evento de cierre.</summary>
public enum MovePendingSalesMode
{
    /// <summary>Mover a un evento de pago existente (no cancelado, no procesado).</summary>
    Existing = 1,
    /// <summary>Crear un nuevo evento de pago con nueva fecha de entrega y límite.</summary>
    New = 2,
}

/// <summary>
/// Mueve todas las ventas con estatus Pendiente (aún sin comprobante) de un evento de
/// cierre a otro evento existente o a uno nuevo, antes de marcar el origen "en proceso
/// de entrega". Así se evita que queden ventas pendientes en un evento ya despachado.
/// </summary>
public record MovePendingSalesCommand(
    int ClosureEventId,
    MovePendingSalesMode Mode,
    int? TargetClosureEventId = null,
    DateTime? NewDeliveryDate = null,
    DateTime? NewPaymentDeadline = null) : IRequest<MovePendingSalesResultDto>;

public class MovePendingSalesResultDto
{
    public int MovedCount { get; set; }
    public int TargetClosureEventId { get; set; }
}