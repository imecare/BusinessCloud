using BusinessCloud.Domain.Bazares.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Common.Interfaces;

public interface IBazaresDbContext
{
    DbSet<BzaCollectorGroup> CollectorGroups { get; }
    DbSet<BzaCollector> Collectors { get; }
    DbSet<BzaCustomer> Customers { get; }
    DbSet<BzaDate> Dates { get; }
    DbSet<BzaSale> Sales { get; }
    DbSet<BzaSoldProduct> SoldProducts { get; }
    DbSet<BzaPayment> Payments { get; }
    DbSet<BzaDispatchSheet> DispatchSheets { get; }
    DbSet<BzaDispatchItem> DispatchItems { get; }
    DbSet<BzaDelivery> Deliveries { get; }
    DbSet<BzaDeliveryItem> DeliveryItems { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}