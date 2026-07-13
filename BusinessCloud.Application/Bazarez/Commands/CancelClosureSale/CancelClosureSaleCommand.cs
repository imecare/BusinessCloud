using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.CancelClosureSale;

/// <summary>
/// Cancela la venta de un cliente dentro de un Evento de Cierre durante la validación
/// de comprobantes (p. ej. porque no se recibió el pago). El bazar captura un motivo e
/// indica si la cancelación es responsabilidad del cliente.
/// </summary>
public record CancelClosureSaleCommand(int ClosureCustomerTotalId, string Reason, bool IsCustomerFault)
    : IRequest<CancelClosureSaleResultDto>;

public class CancelClosureSaleResultDto
{
    public int ClosureEventId { get; set; }
    public int ClosureCustomerTotalId { get; set; }
    /// <summary>Estado resultante del total del cliente (5 = Cancelada).</summary>
    public int TotalStatus { get; set; }
    /// <summary>Estado resultante del evento de pago.</summary>
    public int ClosureStatus { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool IsCustomerFault { get; set; }
}
