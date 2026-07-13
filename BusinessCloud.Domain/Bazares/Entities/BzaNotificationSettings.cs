using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Bazares.Entities;

/// <summary>
/// Configuración de notificaciones del módulo Bazares (una por tenant).
/// Contiene las plantillas de mensajes personalizados que se envían a los clientes.
/// </summary>
public class BzaNotificationSettings : BaseAuditableEntity
{
    public int Id { get; set; }

    /// <summary>Mensaje de cobro personalizado.</summary>
    public string ChargeMessage { get; set; } = string.Empty;

    /// <summary>Mensaje de pago a vencer personalizado.</summary>
    public string PaymentDueSoonMessage { get; set; } = string.Empty;

    /// <summary>Mensaje de pago vencido personalizado.</summary>
    public string PaymentOverdueMessage { get; set; } = string.Empty;

    /// <summary>Mensaje de venta cancelada personalizado.</summary>
    public string SaleCancelledMessage { get; set; } = string.Empty;
}
