using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Bazares.Entities;

/// <summary>
/// Registro de un mensaje de WhatsApp enviado por la Cloud API y su estatus de entrega,
/// actualizado por los webhooks de Meta. Permite mostrar al bazar si el mensaje llegó al
/// cliente o el motivo por el que no se entregó (por ejemplo, número sin WhatsApp).
/// </summary>
public class BzaWhatsAppMessage : BaseAuditableEntity
{
    public int Id { get; set; }

    /// <summary>Id del mensaje devuelto por Meta (wamid). Se usa para correlacionar el estatus del webhook.</summary>
    public string? WaMessageId { get; set; }

    /// <summary>Teléfono destino (solo dígitos, con lada).</summary>
    public string ToPhone { get; set; } = string.Empty;

    /// <summary>Propósito del mensaje: "otp", "charge", "totals", etc.</summary>
    public string Purpose { get; set; } = string.Empty;

    /// <summary>Cliente relacionado (opcional).</summary>
    public int? BzaCustomerId { get; set; }

    /// <summary>Total de cierre relacionado (opcional), para el reporte de comprobantes.</summary>
    public int? BzaClosureCustomerTotalId { get; set; }

    /// <summary>Estatus: accepted/sent/delivered/read/failed.</summary>
    public string Status { get; set; } = "sent";

    /// <summary>Código de error de Meta cuando el estatus es "failed" (por ejemplo 131026 = no tiene WhatsApp).</summary>
    public int? ErrorCode { get; set; }

    /// <summary>Título del error de Meta.</summary>
    public string? ErrorTitle { get; set; }

    /// <summary>Detalle del error de Meta.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Fecha en que se envió (o registró) el mensaje.</summary>
    public DateTime SentAt { get; set; }

    /// <summary>Fecha del último estatus recibido del webhook.</summary>
    public DateTime? StatusUpdatedAt { get; set; }
}
