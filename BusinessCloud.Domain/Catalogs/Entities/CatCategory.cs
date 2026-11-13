using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Catalogs.Entities;

/// <summary>Categoría del catálogo. Una subcategoría tiene <see cref="ParentId"/> (solo 2 niveles).</summary>
public class CatCategory : BaseAuditableEntity
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? ImageBlobName { get; set; }
    public int SortOrder { get; set; }
    public bool IsVisible { get; set; } = true;
}
