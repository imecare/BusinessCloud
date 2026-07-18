using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Bazares.Entities;

/// <summary>
/// Suscripcion Web Push de un cliente para recibir notificaciones del bazar.
/// </summary>
public class BzaCustomerNotificationSubscription : BaseAuditableEntity
{
    public int Id { get; set; }

    public int BzaCustomerId { get; set; }
    public BzaCustomer Customer { get; set; } = null!;

    /// <summary>Ultimo total de cierre vinculado al momento de registrar la suscripcion (opcional).</summary>
    public int? BzaClosureCustomerTotalId { get; set; }
    public BzaClosureCustomerTotal? ClosureCustomerTotal { get; set; }

    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public DateTime? LastSuccessfulPushAt { get; set; }
    public DateTime? LastFailedPushAt { get; set; }
    public string? LastFailureReason { get; set; }
}
