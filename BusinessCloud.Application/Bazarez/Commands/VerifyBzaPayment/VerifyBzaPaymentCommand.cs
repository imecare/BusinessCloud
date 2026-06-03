using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.VerifyBzaPayment;

/// <summary>
/// Comando para aprobar o rechazar un pago preautorizado.
/// </summary>
public record VerifyBzaPaymentCommand : IRequest<VerifyBzaPaymentResult>
{
    public int PaymentId { get; init; }

    /// <summary>
    /// true = Aprobar, false = Rechazar
    /// </summary>
    public bool Approved { get; init; }

    /// <summary>
    /// Notas del responsable sobre la verificación.
    /// </summary>
    public string? Notes { get; init; }
}

/// <summary>
/// Resultado de la verificación del pago.
/// </summary>
public record VerifyBzaPaymentResult(
    bool Success,
    string Message,
    string NewCustomerStatus // "Pagado" o "Pendiente" para el cliente en este evento
);
