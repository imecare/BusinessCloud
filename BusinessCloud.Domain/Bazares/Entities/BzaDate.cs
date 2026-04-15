using BusinessCloud.Domain.Common;
using BusinessCloud.Domain.Common.Entities;

namespace BusinessCloud.Domain.Bazares.Entities;

public class BzaDate : BaseAuditableEntity
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty; // "PAGO" o "ENTREGA"
    public string Description { get; set; } = string.Empty;
}