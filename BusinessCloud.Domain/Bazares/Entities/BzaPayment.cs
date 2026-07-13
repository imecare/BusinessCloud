using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Bazares.Entities;

/// <summary>
/// Pago/abono registrado de un cliente para sus compras dentro de un Evento de Venta.
/// </summary>
public class BzaPayment : BaseAuditableEntity
{
    public int Id { get; set; }

    /// <summary>
    /// Monto del pago/abono.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Fecha en que se registró el pago.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Método de pago: "Efectivo", "Transferencia", "Deposito".
    /// </summary>
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>
    /// URL del comprobante de pago almacenado en BlobStorage.
    /// </summary>
    public string? ProofImageUrl { get; set; }

    /// <summary>
    /// Referencia de la transferencia/depósito.
    /// </summary>
    public string? Reference { get; set; }

    /// <summary>
    /// Estado del pago:
    /// 1=Preautorizado (pendiente revisión), 2=Aprobado, 3=Rechazado
    /// </summary>
    public int PaymentStatus { get; set; } = 1;

    /// <summary>
    /// true si PaymentStatus == 2 (Aprobado). Mantenido por compatibilidad.
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// Notas del responsable al aprobar/rechazar el pago.
    /// </summary>
    public string? VerificationNotes { get; set; }

    /// <summary>
    /// Fecha de verificación del pago.
    /// </summary>
    public DateTime? VerifiedAt { get; set; }

    // ─────────────────────────────────────────────────────────────────────────
    // Relaciones
    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// FK al Evento de Venta donde se registra este pago.
    /// </summary>
    public int BzaEventId { get; set; }
    public BzaEvent Event { get; set; } = null!;

    /// <summary>
    /// FK al Cliente que realiza el pago.
    /// </summary>
    public int BzaCustomerId { get; set; }
    public BzaCustomer Customer { get; set; } = null!;
}
