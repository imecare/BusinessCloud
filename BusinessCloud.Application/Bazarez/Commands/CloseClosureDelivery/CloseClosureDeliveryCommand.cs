using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.CloseClosureDelivery;

/// <summary>
/// Cierra la entrega de un Evento de Cierre: requiere que ya esté en proceso de
/// entrega y que tenga al menos un comprobante de entrega subido. A partir de aquí,
/// los clientes ven su comprobante de entrega en lugar de las opciones de pago.
/// </summary>
public record CloseClosureDeliveryCommand(int ClosureEventId) : IRequest<CloseClosureDeliveryResultDto>;

public class CloseClosureDeliveryResultDto
{
    public bool Success { get; set; }
    public bool Delivered { get; set; }
    public DateTime? DeliveredAt { get; set; }
}