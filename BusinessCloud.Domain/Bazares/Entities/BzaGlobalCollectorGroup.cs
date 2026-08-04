namespace BusinessCloud.Domain.Bazares.Entities;

/// <summary>Catálogo global de grupos disponible para todos los tenants.</summary>
public class BzaGlobalCollectorGroup
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? DeliveryFrequency { get; set; }
    public int? DeliveryDay { get; set; }
    public ICollection<BzaGlobalCollector> Collectors { get; set; } = new List<BzaGlobalCollector>();
}
