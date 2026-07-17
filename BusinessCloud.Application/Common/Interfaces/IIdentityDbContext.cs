using BusinessCloud.Domain.Common.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Common.Interfaces;

/// <summary>
/// Abstracción del contexto de identidad para la capa de aplicación.
/// Expone las entidades de empresas (Tenant), sus módulos y sus suscripciones.
/// No aplica filtro multi-tenant: lo administra el rol global PlatformAdmin.
/// </summary>
public interface IIdentityDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<TenantModule> TenantModules { get; }
    DbSet<TenantSubscription> TenantSubscriptions { get; }
    DbSet<SystemSeller> SystemSellers { get; }
    DbSet<SellerCommission> SellerCommissions { get; }
    DbSet<Package> Packages { get; }
    DbSet<PackagePurchase> PackagePurchases { get; }
    DbSet<TenantMessageBalance> TenantMessageBalances { get; }
    DbSet<PlatformSettings> PlatformSettings { get; }
    DbSet<MessagePackageRequest> MessagePackageRequests { get; }
    DbSet<ContactRequest> ContactRequests { get; }
    DbSet<ApplicationUser> Users { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
