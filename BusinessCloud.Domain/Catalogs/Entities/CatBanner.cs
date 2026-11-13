using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Catalogs.Entities;

/// <summary>Imagen del carrusel de promociones de la página principal.</summary>
public class CatBanner : BaseAuditableEntity
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string BlobName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public int? LinkCategoryId { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
