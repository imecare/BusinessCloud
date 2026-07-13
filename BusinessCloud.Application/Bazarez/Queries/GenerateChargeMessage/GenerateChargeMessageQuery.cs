using MediatR;

namespace BusinessCloud.Application.Bazares.Queries.GenerateChargeMessage;

/// <summary>
/// Genera el texto del mensaje de cobro para un cliente, incluyendo el listado de productos
/// pendientes agrupados por Evento de Venta (venta), los totales y las tarjetas activas de pago.
/// </summary>
public record GenerateChargeMessageQuery(int BzaCustomerId) : IRequest<ChargeMessageResultDto>;

public class ChargeMessageResultDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>Teléfono del cliente (solo dígitos) para construir el enlace de WhatsApp.</summary>
    public string CustomerPhone { get; set; } = string.Empty;

    /// <summary>Total pendiente del cliente en todos sus eventos.</summary>
    public decimal TotalPending { get; set; }

    /// <summary>true si el cliente tiene saldo pendiente.</summary>
    public bool HasPending { get; set; }

    /// <summary>Texto del mensaje listo para enviar/copiar.</summary>
    public string Message { get; set; } = string.Empty;
}
