using BusinessCloud.Domain.Common;
using BusinessCloud.Domain.Common.Entities;

namespace BusinessCloud.Domain.Bazares.Entities;

public class BzaSale : BaseAuditableEntity
{
    public int Id { get; set; }
    public string? Description { get; set; }
    public decimal Total { get; set; }
    public int Status { get; set; } // 1: Pendiente, 2: Pagado

    public int BzaCustomerId { get; set; }
    public BzaCustomer Customer { get; set; } = null!;
    public ICollection<BzaProduct> Products { get; set; } = new List<BzaProduct>();
}