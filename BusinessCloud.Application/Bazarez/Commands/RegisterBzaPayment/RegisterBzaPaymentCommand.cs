using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.RegisterBzaPayment;

/// <summary>
/// Comando para registrar abono/pago de un cliente para sus compras de un Evento de Venta.
/// </summary>
public record RegisterBzaPaymentCommand : IRequest<BzaPaymentResultDto>
{
    /// <summary>
    /// FK al Evento de Venta donde se registra el pago.
    /// </summary>
    public int BzaSaleId { get; init; }

    /// <summary>
    /// FK al Cliente que realiza el pago.
    /// </summary>
    public int BzaCustomerId { get; init; }

    /// <summary>
    /// Monto del pago/abono.
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Método de pago: "Efectivo", "Transferencia", "Deposito".
    /// </summary>
    public string PaymentMethod { get; init; } = string.Empty;

    /// <summary>
    /// Referencia de la transferencia/depósito (opcional).
    /// </summary>
    public string? Reference { get; init; }
}

/// <summary>
/// Resultado del registro de pago.
/// </summary>
public class BzaPaymentResultDto
{
    public int PaymentId { get; set; }
    public decimal CustomerPendingBalanceInEvent { get; set; }
    public bool IsFullyPaid { get; set; }
}
