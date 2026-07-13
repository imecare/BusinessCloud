using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.RejectClosureProof;

/// <summary>
/// Rechaza el comprobante recibido de un cliente dentro de un Evento de Cierre.
/// El bazar captura un motivo que el cliente podrá consultar en su enlace para
/// volver a subir un comprobante.
/// </summary>
public record RejectClosureProofCommand(int ClosureCustomerTotalId, string Reason)
    : IRequest<RejectClosureProofResultDto>;

public class RejectClosureProofResultDto
{
    public int ClosureEventId { get; set; }
    public int ClosureCustomerTotalId { get; set; }
    /// <summary>Estado resultante del total del cliente (4 = Rechazado).</summary>
    public int TotalStatus { get; set; }
    /// <summary>Estado resultante del evento de pago.</summary>
    public int ClosureStatus { get; set; }
    /// <summary>Motivo del rechazo registrado.</summary>
    public string RejectionReason { get; set; } = string.Empty;
}
