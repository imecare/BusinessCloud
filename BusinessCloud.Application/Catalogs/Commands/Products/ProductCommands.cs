using BusinessCloud.Application.Catalogs.Common;
using BusinessCloud.Application.Catalogs.Dtos;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Catalogs.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Catalogs.Commands.Products;

public record CreateProductCommand(ProductInput Input) : IRequest<ProductDto>;
public record UpdateProductCommand(int Id, ProductInput Input) : IRequest<ProductDto>;
public record DeleteProductCommand(int Id) : IRequest;
public record SetProductStatusCommand(int Id, string Status) : IRequest;
public record SetProductFeaturedCommand(int Id, bool IsFeatured) : IRequest;
public record SetVariantStockCommand(int ProductId, int VariantId, int Stock) : IRequest;
public record UploadProductImageCommand(int ProductId, Stream Content) : IRequest<ProductImageDto>;
public record DeleteProductImageCommand(int ProductId, int ImageId) : IRequest;
public record ReorderProductImagesCommand(int ProductId, IReadOnlyList<int> ImageIds) : IRequest;

public static class ProductRules
{
    public const int MaxImages = 5;
    public const int MaxStock = 1_000_000;

    public static CatProductStatus ParseStatus(string? status) =>
        Enum.TryParse<CatProductStatus>(status, true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed
            : throw new ArgumentException("Estado de producto inválido. Usa Active, SoldOut o Hidden.");

    public static void ValidateStock(int stock)
    {
        if (stock is < 0 or > MaxStock)
            throw new ArgumentException($"La existencia debe estar entre 0 y {MaxStock}.");
    }

    public static async Task ValidateAsync(
        ICatalogsDbContext db, string tenantId, ProductInput input, CancellationToken ct)
    {
        if (CatalogText.Clean(input.Name, 200) is null)
            throw new ArgumentException("El nombre del producto es obligatorio.");
        if (input.Name.Trim().Length > 200)
            throw new ArgumentException("El nombre del producto no debe exceder 200 caracteres.");
        if (input.Description is { Length: > 4000 })
            throw new ArgumentException("La descripción no debe exceder 4000 caracteres.");
        if (input.Price is < 0 or > 9_999_999)
            throw new ArgumentException("El precio debe ser mayor o igual a 0.");
        ParseStatus(input.Status);

        if (input.Variants.Count > 100)
            throw new ArgumentException("Un producto admite máximo 100 variantes.");
        foreach (var v in input.Variants)
        {
            ValidateStock(v.Stock);
            if (v.Size is { Length: > 50 } || v.Color is { Length: > 50 })
                throw new ArgumentException("Talla y color no deben exceder 50 caracteres.");
            if (v.Sku is { Length: > 60 })
                throw new ArgumentException("El SKU no debe exceder 60 caracteres.");
        }

        var keys = input.Variants.Select(v => $"{v.Size?.Trim().ToLowerInvariant()}|{v.Color?.Trim().ToLowerInvariant()}").ToList();
        if (keys.Count != keys.Distinct().Count())
            throw new ArgumentException("Hay variantes repetidas (misma talla y color).");

        if (input.CategoryId is not null
            && !await db.Categories.AnyAsync(c => c.Id == input.CategoryId && c.TenantId == tenantId, ct))
            throw new KeyNotFoundException("La categoría no existe.");
    }
}

public class CreateProductHandler(ICatalogsDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<CreateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var tenantId = currentUser.GetRequiredTenantId();
        var input = request.Input;
        await ProductRules.ValidateAsync(db, tenantId, input, ct);

        var product = new CatProduct
        {
            TenantId = tenantId,
            Name = input.Name.Trim(),
            Description = CatalogText.Clean(input.Description, 4000),
            CategoryId = input.CategoryId,
            Price = input.Price,
            Status = ProductRules.ParseStatus(input.Status),
            IsFeatured = input.IsFeatured,
        };

        var variants = input.Variants.Count > 0 ? input.Variants : [new VariantInput()];
        foreach (var v in variants)
        {
            product.Variants.Add(new CatProductVariant
            {
                TenantId = tenantId,
                Size = CatalogText.Clean(v.Size, 50),
                Color = CatalogText.Clean(v.Color, 50),
                Sku = CatalogText.Clean(v.Sku, 60),
                Stock = v.Stock,
            });
        }

        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        return CatalogMappers.ToDto(product);
    }
}

public class UpdateProductHandler(ICatalogsDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<UpdateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        var tenantId = currentUser.GetRequiredTenantId();
        var input = request.Input;
        await ProductRules.ValidateAsync(db, tenantId, input, ct);

        var product = await db.Products
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.TenantId == tenantId && !p.IsDeleted, ct)
            ?? throw new KeyNotFoundException("El producto no existe.");

        product.Name = input.Name.Trim();
        product.Description = CatalogText.Clean(input.Description, 4000);
        product.CategoryId = input.CategoryId;
        product.Price = input.Price;
        product.Status = ProductRules.ParseStatus(input.Status);
        product.IsFeatured = input.IsFeatured;

        var inputVariants = input.Variants.Count > 0 ? input.Variants : [new VariantInput()];
        var keptIds = new HashSet<int>();

        foreach (var v in inputVariants)
        {
            CatProductVariant? variant = null;
            if (v.Id is { } id)
            {
                variant = product.Variants.FirstOrDefault(x => x.Id == id)
                    ?? throw new KeyNotFoundException("Una variante no pertenece a este producto.");
            }
            else
            {
                variant = new CatProductVariant { TenantId = tenantId };
                product.Variants.Add(variant);
            }

            variant.Size = CatalogText.Clean(v.Size, 50);
            variant.Color = CatalogText.Clean(v.Color, 50);
            variant.Sku = CatalogText.Clean(v.Sku, 60);
            variant.Stock = v.Stock;
            variant.IsActive = true;
            if (variant.Id != 0) keptIds.Add(variant.Id);
        }

        var removed = product.Variants.Where(v => v.Id != 0 && !keptIds.Contains(v.Id)).ToList();
        if (removed.Count > 0)
        {
            var removedIds = removed.Select(v => v.Id).ToList();
            var referenced = (await db.OrderItems
                .Where(i => i.TenantId == tenantId && removedIds.Contains(i.VariantId))
                .Select(i => i.VariantId).Distinct().ToListAsync(ct)).ToHashSet();

            foreach (var variant in removed)
            {
                // Una variante usada en pedidos solo se desactiva para conservar el historial.
                if (referenced.Contains(variant.Id)) variant.IsActive = false;
                else db.ProductVariants.Remove(variant);
            }
        }

        await db.SaveChangesAsync(ct);
        return CatalogMappers.ToDto(product);
    }
}

public class DeleteProductHandler(ICatalogsDbContext db, ICurrentUserService currentUser, IBlobStorageService blob)
    : IRequestHandler<DeleteProductCommand>
{
    public async Task Handle(DeleteProductCommand request, CancellationToken ct)
    {
        var tenantId = currentUser.GetRequiredTenantId();
        var product = await db.Products.Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.TenantId == tenantId && !p.IsDeleted, ct)
            ?? throw new KeyNotFoundException("El producto no existe.");

        var blobNames = product.Images.Select(i => i.BlobName).ToList();
        product.IsDeleted = true;
        product.IsFeatured = false;
        db.ProductImages.RemoveRange(product.Images);
        await db.SaveChangesAsync(ct);

        foreach (var name in blobNames)
            await CatalogImageUploader.DeleteQuietlyAsync(blob, name, ct);
    }
}

public class SetProductStatusHandler(ICatalogsDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<SetProductStatusCommand>
{
    public async Task Handle(SetProductStatusCommand request, CancellationToken ct)
    {
        var tenantId = currentUser.GetRequiredTenantId();
        var status = ProductRules.ParseStatus(request.Status);
        var product = await db.Products
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.TenantId == tenantId && !p.IsDeleted, ct)
            ?? throw new KeyNotFoundException("El producto no existe.");
        product.Status = status;
        await db.SaveChangesAsync(ct);
    }
}

public class SetProductFeaturedHandler(ICatalogsDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<SetProductFeaturedCommand>
{
    public async Task Handle(SetProductFeaturedCommand request, CancellationToken ct)
    {
        var tenantId = currentUser.GetRequiredTenantId();
        var product = await db.Products
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.TenantId == tenantId && !p.IsDeleted, ct)
            ?? throw new KeyNotFoundException("El producto no existe.");
        product.IsFeatured = request.IsFeatured;
        await db.SaveChangesAsync(ct);
    }
}

public class SetVariantStockHandler(ICatalogsDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<SetVariantStockCommand>
{
    public async Task Handle(SetVariantStockCommand request, CancellationToken ct)
    {
        var tenantId = currentUser.GetRequiredTenantId();
        ProductRules.ValidateStock(request.Stock);
        var variant = await db.ProductVariants.FirstOrDefaultAsync(v =>
                v.Id == request.VariantId && v.ProductId == request.ProductId && v.TenantId == tenantId && v.IsActive, ct)
            ?? throw new KeyNotFoundException("La variante no existe.");
        variant.Stock = request.Stock;
        await db.SaveChangesAsync(ct);
    }
}

public class UploadProductImageHandler(ICatalogsDbContext db, ICurrentUserService currentUser, IBlobStorageService blob)
    : IRequestHandler<UploadProductImageCommand, ProductImageDto>
{
    public async Task<ProductImageDto> Handle(UploadProductImageCommand request, CancellationToken ct)
    {
        var tenantId = currentUser.GetRequiredTenantId();
        var product = await db.Products.Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.TenantId == tenantId && !p.IsDeleted, ct)
            ?? throw new KeyNotFoundException("El producto no existe.");

        if (product.Images.Count >= ProductRules.MaxImages)
            throw new ArgumentException($"Un producto admite máximo {ProductRules.MaxImages} imágenes.");

        var (url, blobName) = await CatalogImageUploader.UploadAsync(blob, tenantId, "products", request.Content, ct);
        var image = new CatProductImage
        {
            TenantId = tenantId,
            ProductId = product.Id,
            Url = url,
            BlobName = blobName,
            SortOrder = product.Images.Count == 0 ? 0 : product.Images.Max(i => i.SortOrder) + 1,
        };
        db.ProductImages.Add(image);
        await db.SaveChangesAsync(ct);
        return new ProductImageDto(image.Id, image.Url, image.SortOrder);
    }
}

public class DeleteProductImageHandler(ICatalogsDbContext db, ICurrentUserService currentUser, IBlobStorageService blob)
    : IRequestHandler<DeleteProductImageCommand>
{
    public async Task Handle(DeleteProductImageCommand request, CancellationToken ct)
    {
        var tenantId = currentUser.GetRequiredTenantId();
        var image = await db.ProductImages.FirstOrDefaultAsync(i =>
                i.Id == request.ImageId && i.ProductId == request.ProductId && i.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException("La imagen no existe.");

        var blobName = image.BlobName;
        db.ProductImages.Remove(image);
        await db.SaveChangesAsync(ct);
        await CatalogImageUploader.DeleteQuietlyAsync(blob, blobName, ct);
    }
}

public class ReorderProductImagesHandler(ICatalogsDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<ReorderProductImagesCommand>
{
    public async Task Handle(ReorderProductImagesCommand request, CancellationToken ct)
    {
        var tenantId = currentUser.GetRequiredTenantId();
        var images = await db.ProductImages
            .Where(i => i.ProductId == request.ProductId && i.TenantId == tenantId).ToListAsync(ct);
        if (images.Count == 0 && request.ImageIds.Count > 0)
            throw new KeyNotFoundException("El producto no tiene imágenes.");

        var order = request.ImageIds.Distinct().ToList();
        if (order.Any(id => images.All(i => i.Id != id)))
            throw new KeyNotFoundException("Alguna imagen no pertenece al producto.");

        var position = 0;
        foreach (var id in order) images.First(i => i.Id == id).SortOrder = position++;
        foreach (var image in images.Where(i => !order.Contains(i.Id))) image.SortOrder = position++;
        await db.SaveChangesAsync(ct);
    }
}
