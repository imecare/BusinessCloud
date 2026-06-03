using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Bazares.Entities;

public class BzaCollectorGroup : BaseAuditableEntity
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<BzaCollector> Collectors { get; set; } = new List<BzaCollector>();
}
