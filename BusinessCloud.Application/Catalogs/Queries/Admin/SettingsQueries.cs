using BusinessCloud.Application.Catalogs.Common;
using BusinessCloud.Application.Catalogs.Dtos;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Catalogs.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Catalogs.Queries.Admin;

public record GetStoreSettingsQuery : IRequest<StoreSettingsDto>;

public class GetStoreSettingsHandler(
    ICatalogsDbContext db, IIdentityDbContext identity, ICurrentUserService currentUser)
    : IRequestHandler<GetStoreSettingsQuery, StoreSettingsDto>
{
    public async Task<StoreSettingsDto> Handle(GetStoreSettingsQuery request, CancellationToken ct)
    {
        var settings = await CatalogSettingsProvider.GetOrCreateAsync(
            db, identity, currentUser.GetRequiredTenantId(), ct);
        return CatalogMappers.ToDto(settings);
    }
}

public record GetCatalogSummaryQuery : IRequest<SummaryDto>;

public class GetCatalogSummaryHandler(ICatalogsDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetCatalogSummaryQuery, SummaryDto>
{
    public async Task<SummaryDto> Handle(GetCatalogSummaryQuery request, CancellationToken ct)
    {
        var tenantId = currentUser.GetRequiredTenantId();
        var products = db.Products.Where(p => p.TenantId == tenantId && !p.IsDeleted);

        var productsCount = await products.CountAsync(ct);
        var hidden = await products.CountAsync(p => p.Status == CatProductStatus.Hidden, ct);
        var soldOut = await products.CountAsync(p => p.Status != CatProductStatus.Hidden
            && (p.Status == CatProductStatus.SoldOut
                || !p.Variants.Any(v => v.IsActive && v.Stock > 0)), ct);
        var newOrders = await db.Orders.CountAsync(o => o.TenantId == tenantId && o.Status == CatOrderStatus.New, ct);
        var interests = await db.InterestRequests.CountAsync(i => i.TenantId == tenantId && !i.IsAttended, ct);

        return new SummaryDto(productsCount, soldOut, hidden, newOrders, interests);
    }
}
