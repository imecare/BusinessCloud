using BusinessCloud.Application.Catalogs.Common;
using BusinessCloud.Application.Catalogs.Dtos;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Catalogs.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Catalogs.Queries.Admin;

public record GetCategoriesQuery : IRequest<IReadOnlyList<CategoryDto>>;
public record GetProductsQuery(string? Search, int? CategoryId, string? Status, bool? Featured, int Page, int PageSize)
    : IRequest<PagedResult<ProductListDto>>;
public record GetProductQuery(int Id) : IRequest<ProductDto>;
public record GetBannersQuery : IRequest<IReadOnlyList<BannerDto>>;
public record GetInterestsQuery(bool? Attended, int Page, int PageSize) : IRequest<PagedResult<InterestDto>>;

internal static class Paging
{
    public static (int Page, int PageSize) Normalize(int page, int pageSize) =>
        (Math.Max(1, page), Math.Clamp(pageSize <= 0 ? 20 : pageSize, 1, 100));
}

public class GetCategoriesHandler(ICatalogsDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    public async Task<IReadOnlyList<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken ct)
    {
        var tenantId = currentUser.GetRequiredTenantId();
        var counts = await db.Products
            .Where(p => p.TenantId == tenantId && !p.IsDeleted && p.CategoryId != null)
            .GroupBy(p => p.CategoryId!.Value)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        var categories = await db.Categories.Where(c => c.TenantId == tenantId)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Id).ToListAsync(ct);

        return categories.Select(c => new CategoryDto(
            c.Id, c.ParentId, c.Name, c.ImageUrl, c.SortOrder, c.IsVisible,
            counts.GetValueOrDefault(c.Id))).ToList();
    }
}

public class GetProductsHandler(ICatalogsDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetProductsQuery, PagedResult<ProductListDto>>
{
    public async Task<PagedResult<ProductListDto>> Handle(GetProductsQuery request, CancellationToken ct)
    {
        var tenantId = currentUser.GetRequiredTenantId();
        var (page, pageSize) = Paging.Normalize(request.Page, request.PageSize);

        var query = db.Products.Where(p => p.TenantId == tenantId && !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term)
                || (p.Description != null && p.Description.ToLower().Contains(term)));
        }
        if (request.CategoryId is { } categoryId)
            query = query.Where(p => p.CategoryId == categoryId);
        if (request.Featured is { } featured)
            query = query.Where(p => p.IsFeatured == featured);
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = ProductRulesStatus(request.Status);
            query = query.Where(p => p.Status == status);
        }

        var total = await query.CountAsync(ct);
        var products = await query
            .Include(p => p.Images).Include(p => p.Variants)
            .OrderByDescending(p => p.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        var categoryIds = products.Where(p => p.CategoryId != null).Select(p => p.CategoryId!.Value).Distinct().ToList();
        var names = await db.Categories.Where(c => c.TenantId == tenantId && categoryIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        var items = products
            .Select(p => CatalogMappers.ToListDto(p, p.CategoryId is { } id ? names.GetValueOrDefault(id) : null))
            .ToList();
        return new PagedResult<ProductListDto>(items, total, page, pageSize);
    }

    private static CatProductStatus ProductRulesStatus(string status) =>
        Enum.TryParse<CatProductStatus>(status, true, out var parsed)
            ? parsed
            : throw new ArgumentException("Estado de producto inválido.");
}

public class GetProductHandler(ICatalogsDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetProductQuery, ProductDto>
{
    public async Task<ProductDto> Handle(GetProductQuery request, CancellationToken ct)
    {
        var tenantId = currentUser.GetRequiredTenantId();
        var product = await db.Products.Include(p => p.Images).Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.TenantId == tenantId && !p.IsDeleted, ct)
            ?? throw new KeyNotFoundException("El producto no existe.");
        return CatalogMappers.ToDto(product);
    }
}

public class GetBannersHandler(ICatalogsDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetBannersQuery, IReadOnlyList<BannerDto>>
{
    public async Task<IReadOnlyList<BannerDto>> Handle(GetBannersQuery request, CancellationToken ct)
    {
        var tenantId = currentUser.GetRequiredTenantId();
        return await db.Banners.Where(b => b.TenantId == tenantId)
            .OrderBy(b => b.SortOrder).ThenBy(b => b.Id)
            .Select(b => new BannerDto(b.Id, b.ImageUrl, b.Title, b.LinkCategoryId, b.SortOrder, b.IsActive))
            .ToListAsync(ct);
    }
}

public class GetInterestsHandler(ICatalogsDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetInterestsQuery, PagedResult<InterestDto>>
{
    public async Task<PagedResult<InterestDto>> Handle(GetInterestsQuery request, CancellationToken ct)
    {
        var tenantId = currentUser.GetRequiredTenantId();
        var (page, pageSize) = Paging.Normalize(request.Page, request.PageSize);

        var query = db.InterestRequests.Where(i => i.TenantId == tenantId);
        if (request.Attended is { } attended)
            query = query.Where(i => i.IsAttended == attended);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(i => i.CreatedAt).ThenByDescending(i => i.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(i => new InterestDto(i.Id, i.ProductId, i.ProductName, i.VariantLabel, i.CustomerName,
                i.CustomerPhone, i.Note, i.IsAttended, i.CreatedAt))
            .ToListAsync(ct);
        return new PagedResult<InterestDto>(items, total, page, pageSize);
    }
}
