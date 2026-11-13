using BusinessCloud.Application.Catalogs.Common;
using BusinessCloud.Application.Catalogs.Dtos;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Catalogs.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Catalogs.Commands.Categories;

public record CreateCategoryCommand(CategoryInput Input) : IRequest<CategoryDto>;
public record UpdateCategoryCommand(int Id, CategoryInput Input) : IRequest<CategoryDto>;
public record DeleteCategoryCommand(int Id) : IRequest;
public record ReorderCategoriesCommand(IReadOnlyList<OrderItemSortDto> Items) : IRequest;
public record UploadCategoryImageCommand(int Id, Stream Content) : IRequest<UploadResultDto>;

public class CreateCategoryHandler(ICatalogsDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    public async Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        var tenantId = currentUser.GetRequiredTenantId();
        var input = request.Input;
        var name = CatalogText.Clean(input.Name, 100) ?? throw new ArgumentException("El nombre de la categoría es obligatorio.");
        await CategoryRules.EnsureValidParentAsync(db, tenantId, null, input.ParentId, ct);

        var siblings = db.Categories.Where(c => c.TenantId == tenantId && c.ParentId == input.ParentId);
        var maxOrder = await siblings.Select(c => (int?)c.SortOrder).MaxAsync(ct) ?? -1;

        var category = new CatCategory
        {
            TenantId = tenantId,
            ParentId = input.ParentId,
            Name = name,
            IsVisible = input.IsVisible,
            SortOrder = maxOrder + 1,
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);
        return new CategoryDto(category.Id, category.ParentId, category.Name, category.ImageUrl,
            category.SortOrder, category.IsVisible, 0);
    }
}

public class UpdateCategoryHandler(ICatalogsDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<UpdateCategoryCommand, CategoryDto>
{
    public async Task<CategoryDto> Handle(UpdateCategoryCommand request, CancellationToken ct)
    {
        var tenantId = currentUser.GetRequiredTenantId();
        var input = request.Input;
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == request.Id && c.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException("La categoría no existe.");
        var name = CatalogText.Clean(input.Name, 100) ?? throw new ArgumentException("El nombre de la categoría es obligatorio.");
        await CategoryRules.EnsureValidParentAsync(db, tenantId, category.Id, input.ParentId, ct);

        category.Name = name;
        category.ParentId = input.ParentId;
        category.IsVisible = input.IsVisible;
        await db.SaveChangesAsync(ct);

        var count = await db.Products.CountAsync(p => p.TenantId == tenantId && !p.IsDeleted && p.CategoryId == category.Id, ct);
        return new CategoryDto(category.Id, category.ParentId, category.Name, category.ImageUrl,
            category.SortOrder, category.IsVisible, count);
    }
}

public class DeleteCategoryHandler(ICatalogsDbContext db, ICurrentUserService currentUser, IBlobStorageService blob)
    : IRequestHandler<DeleteCategoryCommand>
{
    public async Task Handle(DeleteCategoryCommand request, CancellationToken ct)
    {
        var tenantId = currentUser.GetRequiredTenantId();
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == request.Id && c.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException("La categoría no existe.");

        if (await db.Categories.AnyAsync(c => c.TenantId == tenantId && c.ParentId == category.Id, ct))
            throw new InvalidOperationException("La categoría tiene subcategorías. Elimínalas o muévelas primero.");
        if (await db.Products.AnyAsync(p => p.TenantId == tenantId && !p.IsDeleted && p.CategoryId == category.Id, ct))
            throw new InvalidOperationException("La categoría tiene productos. Muévelos a otra categoría primero.");

        // Limpia referencias para no dejar el panel promo o banners apuntando a una categoría inexistente.
        var settings = await db.StoreSettings.FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);
        if (settings is not null)
        {
            if (settings.PromoCategory1Id == category.Id) settings.PromoCategory1Id = null;
            if (settings.PromoCategory2Id == category.Id) settings.PromoCategory2Id = null;
            if (settings.PromoCategory1Id is null || settings.PromoCategory2Id is null)
                settings.PromoPanelEnabled = false;
        }
        var banners = await db.Banners.Where(b => b.TenantId == tenantId && b.LinkCategoryId == category.Id).ToListAsync(ct);
        foreach (var banner in banners) banner.LinkCategoryId = null;

        var blobName = category.ImageBlobName;
        db.Categories.Remove(category);
        await db.SaveChangesAsync(ct);
        await CatalogImageUploader.DeleteQuietlyAsync(blob, blobName, ct);
    }
}

public class ReorderCategoriesHandler(ICatalogsDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<ReorderCategoriesCommand>
{
    public async Task Handle(ReorderCategoriesCommand request, CancellationToken ct)
    {
        var tenantId = currentUser.GetRequiredTenantId();
        var ids = request.Items.Select(i => i.Id).ToList();
        var categories = await db.Categories.Where(c => c.TenantId == tenantId && ids.Contains(c.Id)).ToListAsync(ct);
        if (categories.Count != ids.Distinct().Count())
            throw new KeyNotFoundException("Alguna categoría no existe.");

        foreach (var item in request.Items)
            categories.First(c => c.Id == item.Id).SortOrder = item.SortOrder;
        await db.SaveChangesAsync(ct);
    }
}

public class UploadCategoryImageHandler(ICatalogsDbContext db, ICurrentUserService currentUser, IBlobStorageService blob)
    : IRequestHandler<UploadCategoryImageCommand, UploadResultDto>
{
    public async Task<UploadResultDto> Handle(UploadCategoryImageCommand request, CancellationToken ct)
    {
        var tenantId = currentUser.GetRequiredTenantId();
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == request.Id && c.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException("La categoría no existe.");

        var (url, blobName) = await CatalogImageUploader.UploadAsync(blob, tenantId, "categories", request.Content, ct);
        var previous = category.ImageBlobName;
        category.ImageUrl = url;
        category.ImageBlobName = blobName;
        await db.SaveChangesAsync(ct);
        await CatalogImageUploader.DeleteQuietlyAsync(blob, previous, ct);
        return new UploadResultDto(url);
    }
}

internal static class CategoryRules
{
    /// <summary>Solo 2 niveles: el padre debe ser raíz y una categoría con hijos no puede volverse subcategoría.</summary>
    public static async Task EnsureValidParentAsync(
        ICatalogsDbContext db, string tenantId, int? categoryId, int? parentId, CancellationToken ct)
    {
        if (parentId is null) return;
        if (parentId == categoryId)
            throw new ArgumentException("Una categoría no puede ser su propia subcategoría.");

        var parent = await db.Categories.FirstOrDefaultAsync(c => c.Id == parentId && c.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException("La categoría padre no existe.");
        if (parent.ParentId is not null)
            throw new ArgumentException("Solo se permiten categorías y subcategorías (2 niveles).");

        if (categoryId is not null
            && await db.Categories.AnyAsync(c => c.TenantId == tenantId && c.ParentId == categoryId, ct))
            throw new ArgumentException("Una categoría con subcategorías no puede convertirse en subcategoría.");
    }
}
