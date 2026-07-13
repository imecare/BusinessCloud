using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Bazares.Entities;

/// <summary>
/// Registro histórico (auditoría) de un comprobante rechazado por el bazar.
/// Se conserva aunque el cliente vuelva a subir otro comprobante, para poder
/// generar reportes (p. ej. detectar clientes que reiteran comprobantes inválidos).
/// Los datos del cliente y del evento se guardan como snapshot al momento del rechazo.
/// </summary>
public class BzaProofRejection : BaseAuditableEntity
{
    public int Id { get; set; }

    /// <summary>Total de cierre al que pertenecía el comprobante rechazado.</summary>
    public int BzaClosureCustomerTotalId { get; set; }

    /// <summary>Evento de cierre (envío de totales) asociado.</summary>
    public int BzaClosureEventId { get; set; }

    /// <summary>Cliente al que se le rechazó el comprobante.</summary>
    public int BzaCustomerId { get; set; }

    /// <summary>Nombre del cliente al momento del rechazo (snapshot).</summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>Teléfono del cliente al momento del rechazo (snapshot).</summary>
    public string? CustomerPhone { get; set; }

    /// <summary>Descripción del evento de cierre al momento del rechazo (snapshot).</summary>
    public string? EventDescription { get; set; }

    /// <summary>Monto total del cliente en ese cierre (snapshot).</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Motivo del rechazo capturado por el bazar.</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>Fecha del rechazo.</summary>
    public DateTime RejectedAt { get; set; }

    /// <summary>
    /// URLs de los comprobantes rechazados (separadas por salto de línea),
    /// conservadas como referencia para el reporte.
    /// </summary>
    public string? ProofUrls { get; set; }
}
