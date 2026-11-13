using BusinessCloud.Application.Catalogs.Common;
using BusinessCloud.Application.Catalogs.Dtos;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Catalogs.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Catalogs.Queries.Public;

public record GetPublicStoreQuery(string Slug) : IRequest<PublicStoreDto>;
public record GetPublicCategoriesQuery(string Slug) : IRequest<IReadOnlyList<PublicCategoryDto>>;
public record GetPublicProductsQuery(string Slug, string? Search, int? CategoryId, bool? Featured, int Page, int PageSize)
    : IRequest<PagedResult<PublicProductListDto>>;
public record GetPublicFeaturedQuery(string Slug) : IRequest<IReadOnlyList<PublicProductListDto>>;
public record GetPublicProductQuery(string Slug, int ProductId) : IRequest<PublicProductDto>;

internal static class PublicCatalogData
{
    /// <summary>Ids de categorías visibles (una subcategoría solo es visible si su padre lo es).</summary>
    public static async Task<List<CatCategory>> VisibleCategoriesAsync(
        ICatalogsDbContext db, string tenantId, CancellationToken ct)
    {
        var all = await db.Categories.IgnoreQueryFilters()
            .Where(c => c.TenantId == tenantId && c.IsVisible)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Id)
            .ToListAsync(ct);
        var rootIds = all.Where(c => c.ParentId == null).Select(c => c.Id).ToHashSet();
        return all.Where(c => c.ParentId == null || rootIds.Contains(c.ParentId.Value)).ToList();
    }

    public static async Task<HashSet<int>> VisibleCategoryIdsAsync(
        ICatalogsDbContext db, string tenantId, CancellationToken ct) =>
        (await VisibleCategoriesAsync(db, tenantId, ct)).Select(c => c.Id).ToHashSet();

    public static IQueryable<CatProduct> VisibleProducts(
        ICatalogsDbContext db, string tenantId, IReadOnlyCollection<int> visibleCategoryIds) =>
        db.Products.IgnoreQueryFilters().Where(p =>
            p.TenantId == tenantId && !p.IsDeleted && p.Status != CatProductStatus.Hidden
            && (p.CategoryId == null || visibleCategoryIds.Contains(p.CategoryId.Value)));
}

public class GetPublicStoreHandler(ICatalogsDbContext db, IIdentityDbContext identity)
    : IRequestHandler<GetPublicStoreQuery, PublicStoreDto>
{
    public async Task<PublicStoreDto> Handle(GetPublicStoreQuery request, CancellationToken ct)
    {
        var settings = await PublicCatalogResolver.ResolveAsync(db, identity, request.Slug, ct);
        var categories = await PublicCatalogData.VisibleCategoriesAsync(db, settings.TenantId, ct);
        var visibleIds = categories.Select(c => c.Id).ToHashSet();

        var banners = await db.Banners.IgnoreQueryFilters()
            .Where(b => b.TenantId == settings.TenantId && b.IsActive)
            .OrderBy(b => b.SortOrder).ThenBy(b => b.Id)
            .ToListAsync(ct);

        var promo = new[] { settings.PromoCategory1Id, settings.PromoCategory2Id }
            .Where(id => id.HasValue && visibleIds.Contains(id.Value))
            .Select(id => categories.First(c => c.Id == id!.Value))
            .Select(c => new PublicPromoCategoryDto(c.Id, c.Name, c.ImageUrl))
            .ToList();

        return new PublicStoreDto(
            settings.PublicName, settings.Slug, settings.LogoUrl, settings.PrimaryColor, settings.Phone,
            settings.Whatsapp, settings.Currency,
            string.IsNullOrWhiteSpace(settings.OrderDisclaimer)
                ? CatalogText.DefaultDisclaimer(settings.PublicName)
                : settings.OrderDisclaimer,
            banners.Select(b => new PublicBannerDto(
                b.Id, b.ImageUrl, b.Title,
                b.LinkCategoryId is { } link && visibleIds.Contains(link) ? link : null)).ToList(),
            new PublicPromoPanelDto(settings.PromoPanelEnabled && promo.Count == 2, promo));
    }
}

public class GetPublicCategoriesHandler(ICatalogsDbContext db, IIdentityDbContext identity)
    : IRequestHandler<GetPublicCategoriesQuery, IReadOnlyList<PublicCategoryDto>>
{
    public async Task<IReadOnlyList<PublicCategoryDto>> Handle(GetPublicCategoriesQuery request, CancellationToken ct)
    {
        var settings = await PublicCatalogResolver.ResolveAsync(db, identity, request.Slug, ct);
        var categories = await PublicCatalogData.VisibleCategoriesAsync(db, settings.TenantId, ct);
        var visibleIds = categories.Select(c => c.Id).ToList();

        var counts = await PublicCatalogData.VisibleProducts(db, settings.TenantId, visibleIds)
            .Where(p => p.CategoryId != null)
            .GroupBy(p => p.CategoryId!.Value)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        var result = new List<PublicCategoryDto>();
        foreach (var root in categories.Where(c => c.ParentId == null))
        {
            var children = categories
                .Where(c => c.ParentId == root.Id && counts.GetValueOrDefault(c.Id) > 0)
                .Select(c => new PublicChildCategoryDto(c.Id, c.Name, c.ImageUrl))
                .ToList();

            if (counts.GetValueOrDefault(root.Id) > 0 || children.Count > 0)
                result.Add(new PublicCategoryDto(root.Id, root.Name, root.ImageUrl, children));
        }
        return result;
    }
}

public class GetPublicProductsHandler(ICatalogsDbContext db, IIdentityDbContext identity)
    : IRequestHandler<GetPublicProductsQuery, PagedResult<PublicProductListDto>>
{
    public async Task<PagedResult<PublicProductListDto>> Handle(GetPublicProductsQuery request, CancellationToken ct)
    {
        var settings = await PublicCatalogResolver.ResolveAsync(db, identity, request.Slug, ct);
        var tenantId = settings.TenantId;
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize <= 0 ? 24 : request.PageSize, 1, 60);

        var categories = await PublicCatalogData.VisibleCategoriesAsync(db, tenantId, ct);
        var visibleIds = categories.Select(c => c.Id).ToList();
        var query = PublicCatalogData.VisibleProducts(db, tenantId, visibleIds);

        if (request.CategoryId is { } categoryId)
        {
            if (categories.All(c => c.Id != categoryId))
                return new PagedResult<PublicProductListDto>([], 0, page, pageSize);
            var ids = categories.Where(c => c.Id == categoryId || c.ParentId == categoryId).Select(c => c.Id).ToList();
            query = query.Where(p => p.CategoryId != null && ids.Contains(p.CategoryId.Value));
        }

        if (request.Featured == true)
            query = query.Where(p => p.IsFeatured);

        // Cada palabra debe aparecer en el nombre, la descripción o el nombre de la categoría.
        var terms = (request.Search ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.ToLower()).Take(6).ToList();
        foreach (var term in terms)
        {
            var matchingCategoryIds = categories.Where(c => c.Name.ToLower().Contains(term)).Select(c => c.Id).ToList();
            var t = term;
            query = query.Where(p => p.Name.ToLower().Contains(t)
                || (p.Description != null && p.Description.ToLower().Contains(t))
                || (p.CategoryId != null && matchingCategoryIds.Contains(p.CategoryId.Value)));
        }

        var total = await query.CountAsync(ct);
        var products = await query.Include(p => p.Images).Include(p => p.Variants)
            .OrderBy(p => p.Name).ThenBy(p => p.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        var names = categories.ToDictionary(c => c.Id, c => c.Name);
        var items = products
            .Select(p => CatalogMappers.ToPublicListDto(p, p.CategoryId is { } id ? names.GetValueOrDefault(id) : null))
            .ToList();
        return new PagedResult<PublicProductListDto>(items, total, page, pageSize);
    }
}

public class GetPublicFeaturedHandler(ICatalogsDbContext db, IIdentityDbContext identity)
    : IRequestHandler<GetPublicFeaturedQuery, IReadOnlyList<PublicProductListDto>>
{
    public async Task<IReadOnlyList<PublicProductListDto>> Handle(GetPublicFeaturedQuery request, CancellationToken ct)
    {
        var settings = await PublicCatalogResolver.ResolveAsync(db, identity, request.Slug, ct);
        var categories = await PublicCatalogData.VisibleCategoriesAsync(db, settings.TenantId, ct);
        var products = await PublicCatalogData
            .VisibleProducts(db, settings.TenantId, categories.Select(c => c.Id).ToList())
            .Where(p => p.IsFeatured)
            .Include(p => p.Images).Include(p => p.Variants)
            .OrderBy(p => p.Name).ThenBy(p => p.Id)
            .Take(12).ToListAsync(ct);

        var names = categories.ToDictionary(c => c.Id, c => c.Name);
        return products
            .Select(p => CatalogMappers.ToPublicListDto(p, p.CategoryId is { } id ? names.GetValueOrDefault(id) : null))
            .ToList();
    }
}

public class GetPublicProductHandler(ICatalogsDbContext db, IIdentityDbContext identity)
    : IRequestHandler<GetPublicProductQuery, PublicProductDto>
{
    public async Task<PublicProductDto> Handle(GetPublicProductQuery request, CancellationToken ct)
    {
        var settings = await PublicCatalogResolver.ResolveAsync(db, identity, request.Slug, ct);
        var categories = await PublicCatalogData.VisibleCategoriesAsync(db, settings.TenantId, ct);

        var product = await PublicCatalogData
            .VisibleProducts(db, settings.TenantId, categories.Select(c => c.Id).ToList())
            .Include(p => p.Images).Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, ct)
            ?? throw new KeyNotFoundException("El producto no está disponible.");

        var categoryName = product.CategoryId is { } id ? categories.FirstOrDefault(c => c.Id == id)?.Name : null;
        var forcedSoldOut = product.Status == CatProductStatus.SoldOut;

        return new PublicProductDto(
            product.Id, product.Name, product.Description, product.Price, product.CategoryId, categoryName,
            product.IsSoldOut,
            product.Images.OrderBy(i => i.SortOrder).ThenBy(i => i.Id).Select(i => new PublicImageDto(i.Id, i.Url)).ToList(),
            product.Variants.Where(v => v.IsActive).OrderBy(v => v.Id)
                .Select(v => new PublicVariantDto(v.Id, v.Size, v.Color, v.Stock, v.Stock > 0 && !forcedSoldOut)).ToList());
    }
}
