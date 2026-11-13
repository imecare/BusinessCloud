namespace BusinessCloud.Application.Catalogs.Dtos;

public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

// ---------------- Admin ----------------

public class StoreSettingsDto
{
    public string Slug { get; set; } = string.Empty;
    public string PublicName { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string PrimaryColor { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Whatsapp { get; set; }
    public string? OrderEmail { get; set; }
    public string Currency { get; set; } = "MXN";
    public string OrderDisclaimer { get; set; } = string.Empty;
    public bool PromoPanelEnabled { get; set; }
    public List<int> PromoCategoryIds { get; set; } = [];
    public string PublicUrlPath { get; set; } = string.Empty;
}

public class StoreSettingsInput
{
    public string Slug { get; set; } = string.Empty;
    public string PublicName { get; set; } = string.Empty;
    public string? PrimaryColor { get; set; }
    public string? Phone { get; set; }
    public string? Whatsapp { get; set; }
    public string? OrderEmail { get; set; }
    public string? Currency { get; set; }
    public string? OrderDisclaimer { get; set; }
    public bool PromoPanelEnabled { get; set; }
    public List<int>? PromoCategoryIds { get; set; }
}

public record UploadResultDto(string Url);

public record CategoryDto(
    int Id, int? ParentId, string Name, string? ImageUrl, int SortOrder, bool IsVisible, int ProductCount);

public class CategoryInput
{
    public int? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool IsVisible { get; set; } = true;
}

public record OrderItemSortDto(int Id, int SortOrder);

public record ProductListDto(
    int Id, string Name, int? CategoryId, string? CategoryName, decimal Price, string Status,
    bool IsFeatured, string? MainImageUrl, int TotalStock, bool IsSoldOut);

public record ProductImageDto(int Id, string Url, int SortOrder);

public record ProductVariantDto(int Id, string? Size, string? Color, string? Sku, int Stock);

public record ProductDto(
    int Id, string Name, string? Description, int? CategoryId, decimal Price, string Status,
    bool IsFeatured, IReadOnlyList<ProductImageDto> Images, IReadOnlyList<ProductVariantDto> Variants,
    bool IsSoldOut);

public class VariantInput
{
    public int? Id { get; set; }
    public string? Size { get; set; }
    public string? Color { get; set; }
    public string? Sku { get; set; }
    public int Stock { get; set; }
}

public class ProductInput
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? CategoryId { get; set; }
    public decimal Price { get; set; }
    public string Status { get; set; } = "Active";
    public bool IsFeatured { get; set; }
    public List<VariantInput> Variants { get; set; } = [];
}

public record BannerDto(int Id, string ImageUrl, string? Title, int? LinkCategoryId, int SortOrder, bool IsActive);

public record OrderListDto(
    int Id, string OrderNumber, string CustomerName, string CustomerPhone, string Status,
    decimal EstimatedTotal, decimal FinalTotal, bool HasOutOfStockItems, DateTime CreatedAt);

public record OrderItemDto(
    int Id, int ProductId, int VariantId, string ProductName, string? VariantLabel, string? ImageUrl,
    decimal UnitPrice, int RequestedQuantity, int QuantityDiscounted, int FinalQuantity,
    bool OutOfStockAtSubmit, string? LineNote);

public record OrderDto(
    int Id, string OrderNumber, string CustomerName, string CustomerPhone, string Status,
    decimal EstimatedTotal, decimal FinalTotal, bool HasOutOfStockItems, DateTime CreatedAt,
    string? Note, string? InternalNote, IReadOnlyList<OrderItemDto> Items, bool StockDiscounted);

public class OrderItemAdjustInput
{
    public int Id { get; set; }
    public int FinalQuantity { get; set; }
}

public class OrderAdjustInput
{
    public string? InternalNote { get; set; }
    public List<OrderItemAdjustInput> Items { get; set; } = [];
}

public record InterestDto(
    int Id, int ProductId, string ProductName, string? VariantLabel, string? CustomerName,
    string CustomerPhone, string? Note, bool IsAttended, DateTime CreatedAt);

public record SummaryDto(int ProductsCount, int SoldOutCount, int HiddenCount, int NewOrders, int PendingInterests);

// ---------------- Público ----------------

public record PublicBannerDto(int Id, string ImageUrl, string? Title, int? LinkCategoryId);
public record PublicPromoCategoryDto(int Id, string Name, string? ImageUrl);
public record PublicPromoPanelDto(bool Enabled, IReadOnlyList<PublicPromoCategoryDto> Categories);

public record PublicStoreDto(
    string PublicName, string Slug, string? LogoUrl, string PrimaryColor, string? Phone, string? Whatsapp,
    string Currency, string OrderDisclaimer, IReadOnlyList<PublicBannerDto> Banners, PublicPromoPanelDto PromoPanel);

public record PublicChildCategoryDto(int Id, string Name, string? ImageUrl);
public record PublicCategoryDto(int Id, string Name, string? ImageUrl, IReadOnlyList<PublicChildCategoryDto> Children);

public record PublicProductListDto(
    int Id, string Name, decimal Price, string? MainImageUrl, bool IsSoldOut, bool IsFeatured, string? CategoryName);

public record PublicImageDto(int Id, string Url);
public record PublicVariantDto(int Id, string? Size, string? Color, int Stock, bool InStock);

public record PublicProductDto(
    int Id, string Name, string? Description, decimal Price, int? CategoryId, string? CategoryName,
    bool IsSoldOut, IReadOnlyList<PublicImageDto> Images, IReadOnlyList<PublicVariantDto> Variants);

public class PublicOrderItemInput
{
    public int ProductId { get; set; }
    public int VariantId { get; set; }
    public int Quantity { get; set; }
}

public class PublicOrderInput
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? Note { get; set; }
    public List<PublicOrderItemInput> Items { get; set; } = [];
}

public record PublicOrderResultDto(string OrderNumber, decimal EstimatedTotal, bool HasOutOfStockItems, string Message);

public class PublicInterestInput
{
    public int ProductId { get; set; }
    public int? VariantId { get; set; }
    public string? CustomerName { get; set; }
    public string CustomerPhone { get; set; } = string.Empty;
    public string? Note { get; set; }
}
