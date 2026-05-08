using BusinessCloud.Domain.Bazares.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Common.Interfaces;

public interface IBazaresDbContext
{
    DbSet<BzaCollector> Collectors { get; }
    DbSet<BzaCustomer> Customers { get; }
    DbSet<BzaDate> Dates { get; }
    DbSet<BzaSale> Sales { get; }
    DbSet<BzaProduct> Products { get; }
    DbSet<BzaPayment> Payments { get; }
    DbSet<BzaDispatchSheet> DispatchSheets { get; }
    DbSet<BzaDispatchItem> DispatchItems { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}