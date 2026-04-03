using BusinessCloud.Domain.Common;
using BusinessCloud.Domain.Payments.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BusinessCloud.Infrastructure.Data;

public class PaymentsDbContext : DbContext
{
    private readonly ICurrentUserService _currentUser;

    public PaymentsDbContext(
        DbContextOptions<PaymentsDbContext> options,
        ICurrentUserService currentUser)
        : base(options)
    {
        _currentUser = currentUser;
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Configuración de Customer
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.RFC).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Phone).HasMaxLength(20).IsRequired();
            entity.HasIndex(e => new { e.RFC, e.Phone }); // Índice para búsquedas rápidas
        });

        // Configuración de Sale
        modelBuilder.Entity<Sale>(entity =>
        {
            entity.HasKey(e => e.Id);
            // Configuración Senior: Siempre definir precisión para decimales
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.ProductCost).HasPrecision(18, 2);
            entity.Property(e => e.CommissionAmount).HasPrecision(18, 2);

            entity.HasOne(d => d.Customer)
                .WithMany(p => p.Sales)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.Restrict); // Evita borrado en cascada accidental
        });

        // Configuración de Payment
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);

            entity.HasOne(d => d.Sale)
                .WithMany(p => p.Payments)
                .HasForeignKey(d => d.SaleId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<IAuditableEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.CreatedBy = _currentUser.Username ?? "SYSTEM";
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedBy = _currentUser.Username ?? "SYSTEM";
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}

public class PaymentsDbContextFactory : IDesignTimeDbContextFactory<PaymentsDbContext>
{
    public PaymentsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PaymentsDbContext>();

        // Asegúrate de que apunte a la base de datos de Payments
        optionsBuilder.UseSqlServer(
            "Server=LAPTOP-5L4BL4RK\\SQLEXPRESS;Database=Payments;Trusted_Connection=True;TrustServerCertificate=True");

        return new PaymentsDbContext(
            optionsBuilder.Options,
            new DummyCurrentUserService());
    }
}