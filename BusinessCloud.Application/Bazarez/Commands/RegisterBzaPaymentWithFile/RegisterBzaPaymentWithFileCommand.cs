using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.RegisterBzaPaymentWithFile;

/// <summary>
/// Comando para registrar pago con comprobante físico a BlobStorage.
/// El pago queda en status "Preautorizado" hasta verificación.
/// </summary>
public record RegisterBzaPaymentWithFileCommand : IRequest<BzaPaymentWithFileResultDto>
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

    /// <summary>
    /// Contenido del archivo de comprobante.
    /// </summary>
    public byte[]? ProofFileContent { get; init; }

    /// <summary>
    /// Nombre del archivo de comprobante.
    /// </summary>
    public string? ProofFileName { get; init; }

    /// <summary>
    /// Content-Type del archivo de comprobante.
    /// </summary>
    public string? ProofContentType { get; init; }
}

/// <summary>
/// Resultado del registro de pago con comprobante.
/// </summary>
public class BzaPaymentWithFileResultDto
{
    public int PaymentId { get; set; }
    public decimal CustomerPendingBalanceInEvent { get; set; }
    public bool IsFullyPaid { get; set; }
    public string? ProofImageUrl { get; set; }
    public int PaymentStatus { get; set; }
    public string PaymentStatusName { get; set; } = string.Empty;
}
