namespace BusinessCloud.Domain.Bazares.Entities;

/// <summary>Recolector de solo lectura perteneciente al catálogo global.</summary>
public class BzaGlobalCollector
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int BzaGlobalCollectorGroupId { get; set; }
    public BzaGlobalCollectorGroup CollectorGroup { get; set; } = null!;
}
