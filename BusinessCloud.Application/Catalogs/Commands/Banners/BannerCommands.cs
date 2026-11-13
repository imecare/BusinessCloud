using BusinessCloud.Application.Catalogs.Common;
using BusinessCloud.Application.Catalogs.Dtos;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Catalogs.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Catalogs.Commands.Banners;

public record CreateBannerCommand(Stream Content, string? Title, int? LinkCategoryId) : IRequest<BannerDto>;
public record UpdateBannerCommand(int Id, string? Title, int? LinkCategoryId, int SortOrder, bool IsActive) : IRequest<BannerDto>;
public record DeleteBannerCommand(int Id) : IRequest;

public class CreateBannerHandler(ICatalogsDbContext db, ICurrentUserService currentUser, IBlobStorageService blob)
    : IRequestHandler<CreateBannerCommand, BannerDto>
{
    private const int MaxBanners = 10;

    public async Task<BannerDto> Handle(CreateBannerCommand request, CancellationToken ct)
    {
        var tenantId = currentUser.GetRequiredTenantId();

        if (await db.Banners.CountAsync(b => b.TenantId == tenantId, ct) >= MaxBanners)
            throw new ArgumentException($"El carrusel admite máximo {MaxBanners} imágenes.");
        await BannerRules.EnsureCategoryAsync(db, tenantId, request.LinkCategoryId, ct);

        var (url, blobName) = await CatalogImageUploader.UploadAsync(blob, tenantId, "banners", request.Content, ct);
        var maxOrder = await db.Banners.Where(b => b.TenantId == tenantId).Select(b => (int?)b.SortOrder).MaxAsync(ct) ?? -1;

        var banner = new CatBanner
        {
            TenantId = tenantId,
            ImageUrl = url,
            BlobName = blobName,
            Title = CatalogText.Clean(request.Title, 120),
            LinkCategoryId = request.LinkCategoryId,
            SortOrder = maxOrder + 1,
        };
        db.Banners.Add(banner);
        await db.SaveChangesAsync(ct);
        return new BannerDto(banner.Id, banner.ImageUrl, banner.Title, banner.LinkCategoryId, banner.SortOrder, banner.IsActive);
    }
}

public class UpdateBannerHandler(ICatalogsDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<UpdateBannerCommand, BannerDto>
{
    public async Task<BannerDto> Handle(UpdateBannerCommand request, CancellationToken ct)
    {
        var tenantId = currentUser.GetRequiredTenantId();
        var banner = await db.Banners.FirstOrDefaultAsync(b => b.Id == request.Id && b.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException("El banner no existe.");
        await BannerRules.EnsureCategoryAsync(db, tenantId, request.LinkCategoryId, ct);

        banner.Title = CatalogText.Clean(request.Title, 120);
        banner.LinkCategoryId = request.LinkCategoryId;
        banner.SortOrder = request.SortOrder;
        banner.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
        return new BannerDto(banner.Id, banner.ImageUrl, banner.Title, banner.LinkCategoryId, banner.SortOrder, banner.IsActive);
    }
}

public class DeleteBannerHandler(ICatalogsDbContext db, ICurrentUserService currentUser, IBlobStorageService blob)
    : IRequestHandler<DeleteBannerCommand>
{
    public async Task Handle(DeleteBannerCommand request, CancellationToken ct)
    {
        var tenantId = currentUser.GetRequiredTenantId();
        var banner = await db.Banners.FirstOrDefaultAsync(b => b.Id == request.Id && b.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException("El banner no existe.");

        var blobName = banner.BlobName;
        db.Banners.Remove(banner);
        await db.SaveChangesAsync(ct);
        await CatalogImageUploader.DeleteQuietlyAsync(blob, blobName, ct);
    }
}

internal static class BannerRules
{
    public static async Task EnsureCategoryAsync(ICatalogsDbContext db, string tenantId, int? categoryId, CancellationToken ct)
    {
        if (categoryId is not null && !await db.Categories.AnyAsync(c => c.Id == categoryId && c.TenantId == tenantId, ct))
            throw new KeyNotFoundException("La categoría del banner no existe.");
    }
}
