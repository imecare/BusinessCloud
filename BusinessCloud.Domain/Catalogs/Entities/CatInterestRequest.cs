using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Catalogs.Entities;

/// <summary>Cliente que dejó su contacto por un producto agotado.</summary>
public class CatInterestRequest : BaseAuditableEntity
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int? VariantId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? VariantLabel { get; set; }
    public string? CustomerName { get; set; }
    public string CustomerPhone { get; set; } = string.Empty;
    public string? Note { get; set; }
    public bool IsAttended { get; set; }
}
