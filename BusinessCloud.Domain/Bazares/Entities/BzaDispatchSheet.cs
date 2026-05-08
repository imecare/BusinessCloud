using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Bazares.Entities;

/// <summary>
/// Hoja de despacho semanal por recolector.
/// </summary>
public class BzaDispatchSheet : BaseAuditableEntity
{
    public int Id { get; set; }
    public int BzaCollectorId { get; set; }
    public DateTime DispatchDate { get; set; }
    public int TotalPackages { get; set; }
    public string? CollectorSignatureUrl { get; set; }
    public DateTime? SignedAt { get; set; }
    public int Status { get; set; } = 1; // 1=Pendiente, 2=Firmado

    public BzaCollector Collector { get; set; } = null!;
    public ICollection<BzaDispatchItem> Items { get; set; } = new List<BzaDispatchItem>();
}
