using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.ValidateClosureProof;

/// <summary>
/// Valida el comprobante recibido de un cliente dentro de un Evento de Cierre.
/// Marca el total como validado (venta pagada) aprobando los pagos preautorizados
/// del cliente en los eventos que abarca el cierre. Cuando todos los comprobantes
/// del cierre quedan validados, el evento de pago pasa a "Validado".
/// </summary>
public record ValidateClosureProofCommand(int ClosureCustomerTotalId) : IRequest<ValidateClosureProofResultDto>;

public class ValidateClosureProofResultDto
{
    public int ClosureEventId { get; set; }
    public int ClosureCustomerTotalId { get; set; }
    /// <summary>Estado resultante del total del cliente (3 = Validado).</summary>
    public int TotalStatus { get; set; }
    /// <summary>Estado resultante del evento de pago.</summary>
    public int ClosureStatus { get; set; }
}
