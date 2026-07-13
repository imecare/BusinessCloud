using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Bazares.Entities;

/// <summary>
/// Registro histórico (auditoría) de una venta cancelada por el bazar durante la
/// validación de comprobantes (p. ej. porque no se recibió el pago).
/// Guarda un snapshot del cliente/evento, el motivo y si la cancelación es
/// responsabilidad del cliente, para poder generar reportes sin clasificar
/// injustamente a clientes cuando la causa no fue suya.
/// </summary>
public class BzaSaleCancellation : BaseAuditableEntity
{
    public int Id { get; set; }

    public int BzaClosureCustomerTotalId { get; set; }
    public int BzaClosureEventId { get; set; }
    public int BzaCustomerId { get; set; }

    /// <summary>Nombre del cliente al momento de la cancelación (snapshot).</summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>Teléfono del cliente al momento de la cancelación (snapshot).</summary>
    public string? CustomerPhone { get; set; }

    /// <summary>Descripción del evento de cierre al momento de la cancelación (snapshot).</summary>
    public string? EventDescription { get; set; }

    /// <summary>Monto total del cliente en ese cierre (snapshot).</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Motivo de la cancelación capturado por el bazar.</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>Si la cancelación es responsabilidad del cliente.</summary>
    public bool IsCustomerFault { get; set; }

    /// <summary>Fecha de la cancelación.</summary>
    public DateTime CancelledAt { get; set; }

    /// <summary>URLs de comprobantes asociados (referencia), si los había.</summary>
    public string? ProofUrls { get; set; }
}
