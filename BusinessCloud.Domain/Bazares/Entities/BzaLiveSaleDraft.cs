using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Bazares.Entities;

public class BzaLiveSaleDraft : BaseAuditableEntity
{
    public int Id { get; set; }
    public int BzaEventId { get; set; }
    public BzaEvent Event { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
