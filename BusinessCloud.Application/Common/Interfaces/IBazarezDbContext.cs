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

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}