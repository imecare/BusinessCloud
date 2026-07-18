using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Bazares.Entities;

/// <summary>
/// Registro de auditoria de notificaciones enviadas al cliente.
/// </summary>
public class BzaNotificationLog : BaseAuditableEntity
{
    public int Id { get; set; }

    public int BzaClosureEventId { get; set; }
    public BzaClosureEvent ClosureEvent { get; set; } = null!;

    public int BzaClosureCustomerTotalId { get; set; }
    public BzaClosureCustomerTotal ClosureCustomerTotal { get; set; } = null!;

    public int BzaCustomerId { get; set; }
    public BzaCustomer Customer { get; set; } = null!;

    /// <summary>1=Recordatorio,2=VenceHoy,3=Cancelacion,4=Validado.</summary>
    public int NotificationType { get; set; }

    /// <summary>1=WebPush,2=WhatsApp.</summary>
    public int Channel { get; set; }

    public bool Success { get; set; }
    public DateTime SentAt { get; set; }
    public string? ErrorMessage { get; set; }
}
