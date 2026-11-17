using BusinessCloud.Application.Catalogs.Dtos;
using BusinessCloud.Domain.Catalogs.Entities;

namespace BusinessCloud.Application.Catalogs.Common;

public static class CatalogMappers
{
    public static ProductListDto ToListDto(CatProduct p, string? categoryName) => new(
        p.Id, p.Name, p.CategoryId, categoryName, p.Price, p.Status.ToString(), p.IsFeatured,
        p.Images.OrderBy(i => i.SortOrder).ThenBy(i => i.Id).Select(i => i.Url).FirstOrDefault(),
        p.TotalStock, p.IsSoldOut);

    public static ProductDto ToDto(CatProduct p) => new(
        p.Id, p.Name, p.Description, p.CategoryId, p.Price, p.Status.ToString(), p.IsFeatured,
        p.Images.OrderBy(i => i.SortOrder).ThenBy(i => i.Id)
            .Select(i => new ProductImageDto(i.Id, i.Url, i.SortOrder)).ToList(),
        p.Variants.Where(v => v.IsActive).OrderBy(v => v.Id)
            .Select(v => new ProductVariantDto(v.Id, v.Size, v.Color, v.Sku, v.Stock)).ToList(),
        p.IsSoldOut);

    public static OrderListDto ToListDto(CatOrder o) => new(
        o.Id, o.OrderNumber, o.CustomerName, o.CustomerPhone, o.Status.ToString(),
        o.EstimatedTotal, o.FinalTotal, o.HasOutOfStockItems, o.CreatedAt);

    public static OrderDto ToDto(CatOrder o) => new(
        o.Id, o.OrderNumber, o.CustomerName, o.CustomerPhone, o.Status.ToString(),
        o.EstimatedTotal, o.FinalTotal, o.HasOutOfStockItems, o.CreatedAt, o.Note, o.InternalNote,
        o.Items.OrderBy(i => i.Id).Select(i => new OrderItemDto(
            i.Id, i.ProductId, i.VariantId, i.ProductName, i.VariantLabel, i.ImageUrl, i.UnitPrice,
            i.RequestedQuantity, i.QuantityDiscounted, i.FinalQuantity, i.OutOfStockAtSubmit, i.LineNote)).ToList(),
        o.StockDiscounted);

    public static StoreSettingsDto ToDto(CatStoreSettings s) => new()
    {
        Slug = s.Slug,
        PublicName = s.PublicName,
        LogoUrl = s.LogoUrl,
        PrimaryColor = s.PrimaryColor,
        TitleFont = s.TitleFont.ToString(),
        Phone = s.Phone,
        Whatsapp = s.Whatsapp,
        OrderEmail = s.OrderEmail,
        Currency = s.Currency,
        OrderDisclaimer = string.IsNullOrWhiteSpace(s.OrderDisclaimer)
            ? CatalogText.DefaultDisclaimer(s.PublicName)
            : s.OrderDisclaimer,
        PromoPanelEnabled = s.PromoPanelEnabled,
        PromoCategoryIds = new[] { s.PromoCategory1Id, s.PromoCategory2Id }
            .Where(id => id.HasValue).Select(id => id!.Value).ToList(),
        PublicUrlPath = $"/c/{s.Slug}",
    };

    public static PublicProductListDto ToPublicListDto(CatProduct p, string? categoryName) => new(
        p.Id, p.Name, p.Price,
        p.Images.OrderBy(i => i.SortOrder).ThenBy(i => i.Id).Select(i => i.Url).FirstOrDefault(),
        p.IsSoldOut, p.IsFeatured, categoryName);
}
