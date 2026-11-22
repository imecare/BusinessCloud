using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Catalogs.Entities;

public enum CatProductStatus
{
    /// <summary>Visible y a la venta.</summary>
    Active = 1,

    /// <summary>Marcado agotado por la empresa (visible en el catálogo como "Agotado").</summary>
    SoldOut = 2,

    /// <summary>No se muestra en el catálogo público.</summary>
    Hidden = 3,
}

public class CatProduct : BaseAuditableEntity
{
    public int Id { get; set; }
    public int? CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public CatProductStatus Status { get; set; } = CatProductStatus.Active;
    public bool IsFeatured { get; set; }
    public bool IsDeleted { get; set; }

    public ICollection<CatProductImage> Images { get; set; } = new List<CatProductImage>();
    public ICollection<CatProductVariant> Variants { get; set; } = new List<CatProductVariant>();

    /// <summary>Existencia total de las variantes activas.</summary>
    public int TotalStock => Variants.Where(v => v.IsActive).Sum(v => v.Stock);

    /// <summary>Agotado efectivo: marcado por la empresa o sin existencia.</summary>
    public bool IsSoldOut => Status == CatProductStatus.SoldOut || TotalStock <= 0;
}

public class CatProductImage : BaseAuditableEntity
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string BlobName { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public CatProduct? Product { get; set; }
}

/// <summary>Variante de un producto (talla/color). Todo producto tiene al menos una.</summary>
public class CatProductVariant : BaseAuditableEntity
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string? Size { get; set; }
    public string? Color { get; set; }
    public string? Sku { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; } = true;

    public CatProduct? Product { get; set; }

    public string? Label
    {
        get
        {
            var label = string.Join(" / ", new[] { Size, Color }.Where(s => !string.IsNullOrWhiteSpace(s)));
            return label.Length > 0 ? label : null;
        }
    }
}
