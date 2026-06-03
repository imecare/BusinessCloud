using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Bazares.Entities;

public class BzaCollector : BaseAuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? FacebookName { get; set; }
    public bool IsActive { get; set; } = true;
    public int? BzaCollectorGroupId { get; set; }
    public BzaCollectorGroup? CollectorGroup { get; set; }
    public ICollection<BzaCustomer> Customers { get; set; } = new List<BzaCustomer>();
}