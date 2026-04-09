using BusinessCloud.Domain.Payments.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Common.Interfaces;

public interface IPaymentsDbContext
{
    DbSet<Customer> Customers { get; }
    DbSet<Seller> Sellers { get; }
    DbSet<Sale> Sales { get; }
    DbSet<Payment> Payments { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}