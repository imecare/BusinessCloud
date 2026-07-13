using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.ReactivateClosureSale;

/// <summary>Destino de la reactivación de una venta cancelada.</summary>
public enum ReactivateMode
{
    /// <summary>Mantener en el mismo evento de pago y fecha.</summary>
    Same = 0,
    /// <summary>Mover a un evento de pago existente (con entrega futura).</summary>
    Existing = 1,
    /// <summary>Crear un nuevo evento de pago con nueva fecha de entrega y límite.</summary>
    New = 2,
}

/// <summary>
/// Reactiva una venta cancelada: vuelve al estado Pendiente para que el cliente
/// pueda subir su comprobante. Permite mantenerla en el mismo evento de pago,
/// moverla a uno existente o crear uno nuevo (cuando ya pasó la entrega o se
/// procesaron etiquetas).
/// </summary>
public record ReactivateClosureSaleCommand(
    int ClosureCustomerTotalId,
    ReactivateMode Mode = ReactivateMode.Same,
    int? TargetClosureEventId = null,
    DateTime? NewDeliveryDate = null,
    DateTime? NewPaymentDeadline = null) : IRequest<ReactivateClosureSaleResultDto>;

public class ReactivateClosureSaleResultDto
{
    public int ClosureEventId { get; set; }
    public int ClosureCustomerTotalId { get; set; }
    /// <summary>Nuevo estado del total del cliente (1 = Pendiente).</summary>
    public int TotalStatus { get; set; }
    public int ClosureStatus { get; set; }
}
