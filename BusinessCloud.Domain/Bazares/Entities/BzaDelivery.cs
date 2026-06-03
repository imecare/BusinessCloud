using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Bazares.Entities;

/// <summary>
/// Entrega programada por grupo de recolectores.
/// </summary>
public class BzaDelivery : BaseAuditableEntity
{
    public int Id { get; set; }
    public int BzaCollectorGroupId { get; set; }
    public DateTime DeliveryDate { get; set; }
    /// <summary>
    /// 1=Programada, 2=EnProceso, 3=Completada, 4=Cancelada
    /// </summary>
    public int Status { get; set; } = 1;
    public string? Notes { get; set; }

    public BzaCollectorGroup CollectorGroup { get; set; } = null!;
    public ICollection<BzaDeliveryItem> Items { get; set; } = new List<BzaDeliveryItem>();
}
