using BusinessCloud.Domain.Common;
using BusinessCloud.Domain.Common.Entities;

namespace BusinessCloud.Domain.Bazares.Entities;

public class BzaProduct : BaseAuditableEntity
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }

    public int BzaSaleId { get; set; }
    public BzaSale Sale { get; set; } = null!;
}