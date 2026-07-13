using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Bazares.Entities;

/// <summary>
/// Item asociado a una entrega (ventas incluidas en la entrega).
/// </summary>
public class BzaDeliveryItem : BaseAuditableEntity
{
    public int Id { get; set; }
    public int BzaDeliveryId { get; set; }
    public int BzaEventId { get; set; }
    public bool Delivered { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? Notes { get; set; }

    public BzaDelivery Delivery { get; set; } = null!;
    public BzaEvent Event { get; set; } = null!;
}
