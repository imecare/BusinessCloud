using BusinessCloud.Domain.Catalogs.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Common.Interfaces;

public interface ICatalogsDbContext
{
    DbSet<CatStoreSettings> StoreSettings { get; }
    DbSet<CatCategory> Categories { get; }
    DbSet<CatProduct> Products { get; }
    DbSet<CatProductImage> ProductImages { get; }
    DbSet<CatProductVariant> ProductVariants { get; }
    DbSet<CatBanner> Banners { get; }
    DbSet<CatOrder> Orders { get; }
    DbSet<CatOrderItem> OrderItems { get; }
    DbSet<CatInterestRequest> InterestRequests { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Ejecuta la operación en una transacción serializable (con reintento). La operación debe
    /// volver a leer lo que necesite porque puede ejecutarse más de una vez.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken);
}
