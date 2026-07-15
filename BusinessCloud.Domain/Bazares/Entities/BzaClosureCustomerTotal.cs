using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Bazares.Entities;

/// <summary>
/// Estado del total enviado a un cliente dentro de un cierre.
/// </summary>
public static class BzaClosureCustomerTotalStatus
{
    /// <summary>Total enviado, en espera del comprobante del cliente.</summary>
    public const int Pending = 1;

    /// <summary>El cliente ya subió su comprobante (pendiente de validar).</summary>
    public const int ProofReceived = 2;

    /// <summary>Comprobante validado por el bazar: venta pagada.</summary>
    public const int Validated = 3;

    /// <summary>Comprobante rechazado por el bazar: el cliente debe subir otro.</summary>
    public const int Rejected = 4;

    /// <summary>Venta cancelada por el bazar (no se recibió el pago u otro motivo).</summary>
    public const int Cancelled = 5;
}

/// <summary>
/// Total enviado a un cliente dentro de un Evento de Cierre de Venta.
/// Conserva el monto total enviado, el token público para subir el comprobante,
/// y el comprobante recibido (que genera pagos preautorizados por revisar).
/// </summary>
public class BzaClosureCustomerTotal : BaseAuditableEntity
{
    public int Id { get; set; }

    public int BzaClosureEventId { get; set; }
    public BzaClosureEvent ClosureEvent { get; set; } = null!;

    public int BzaCustomerId { get; set; }
    public BzaCustomer Customer { get; set; } = null!;

    /// <summary>Grupo de recolección del cliente al momento del envío (define su fecha de entrega).</summary>
    public int? BzaCollectorGroupId { get; set; }

    /// <summary>Monto total pendiente enviado al cliente.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Token único público usado en el link para que el cliente suba su comprobante.</summary>
    public string UploadToken { get; set; } = string.Empty;

    /// <summary>
    /// URL del último comprobante subido por el cliente (BlobStorage).
    /// Se conserva por compatibilidad; la lista completa vive en <see cref="Proofs"/>.
    /// </summary>
    public string? ProofImageUrl { get; set; }

    /// <summary>Fecha en que el cliente subió el último comprobante.</summary>
    public DateTime? ProofUploadedAt { get; set; }

    /// <summary>Comprobantes subidos por el cliente (permite varios depósitos).</summary>
    public ICollection<BzaClosureProof> Proofs { get; set; } = new List<BzaClosureProof>();

    /// <summary>Estado: 1=Pendiente, 2=ComprobanteRecibido, 3=Validado, 4=Rechazado.</summary>
    public int Status { get; set; } = BzaClosureCustomerTotalStatus.Pending;

    /// <summary>Método de pago declarado por el cliente: 0=No especificado, 1=Transferencia, 2=Depósito, 3=Retiro sin tarjeta.</summary>
    public int PaymentMethod { get; set; }

    /// <summary>Referencia o aclaración opcional que el cliente agrega al subir su comprobante (p. ej. número de referencia de la transferencia).</summary>
    public string? CustomerReference { get; set; }

    /// <summary>Banco desde el que el cliente realizó el retiro sin tarjeta (solo aplica cuando PaymentMethod = 3).</summary>
    public string? WithdrawalBank { get; set; }

    /// <summary>Indica que el comprobante fue subido por el bazar en nombre del cliente (no por el cliente).</summary>
    public bool ProofUploadedByBazar { get; set; }

    /// <summary>Indica que el total fue validado sin comprobante (el bazar confirmó el pago por otro medio).</summary>
    public bool ValidatedWithoutProof { get; set; }

    /// <summary>Nota del bazar al validar (obligatoria cuando se valida sin comprobante).</summary>
    public string? ValidationNote { get; set; }

    /// <summary>Motivo del rechazo capturado por el bazar (visible para el cliente).</summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Justificación opcional que el cliente agrega al volver a subir un comprobante
    /// después de un rechazo (visible para el bazar en la reautorización).
    /// </summary>
    public string? CustomerJustification { get; set; }

    /// <summary>
    /// Indica que el comprobante fue subido nuevamente tras un rechazo previo.
    /// Permite al bazar identificar reenvíos.
    /// </summary>
    public bool Resubmitted { get; set; }

    /// <summary>Motivo de la cancelación de la venta capturado por el bazar.</summary>
    public string? CancellationReason { get; set; }

    /// <summary>Indica si la cancelación es responsabilidad del cliente.</summary>
    public bool? CancelledIsCustomerFault { get; set; }

    /// <summary>Fecha de la cancelación de la venta.</summary>
    public DateTime? CancelledAt { get; set; }
}
