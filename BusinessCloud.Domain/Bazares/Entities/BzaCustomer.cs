using BusinessCloud.Domain.Common;
using BusinessCloud.Domain.Common.Entities;

namespace BusinessCloud.Domain.Bazares.Entities;

public class BzaCustomer : BaseAuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? FacebookName { get; set; }
    public string Phone { get; set; } = string.Empty;
    public int Status { get; set; } = 1; // 1: Activo, 0: Inactivo
    public string? PortalToken { get; set; } // Token único para portal de auto-gestión

    public int BzaCollectorId { get; set; }
    public BzaCollector Collector { get; set; } = null!;
    public ICollection<BzaSale> Sales { get; set; } = new List<BzaSale>();
}