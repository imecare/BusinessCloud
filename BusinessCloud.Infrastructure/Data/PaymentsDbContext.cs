using BusinessCloud.Domain.Common;
using BusinessCloud.Domain.Payments.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BusinessCloud.Infrastructure.Data;

public class PaymentsDbContext : DbContext
{
    private readonly int _currentTenantId;
    private readonly string _currentUserId;

    public PaymentsDbContext(
        DbContextOptions<PaymentsDbContext> options,
        ICurrentUserService userService) : base(options)
    {
      // Obtenemos los datos del usuario logueado (vía JWT) [cite: 43]
        _currentTenantId = userService.TenantId;
        _currentUserId = userService.UserId ?? "System";
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


       // 1. FILTRO GLOBAL SAAS: Ninguna empresa verá datos de otra 
        modelBuilder.Entity<Sale>().HasQueryFilter(s => s.TenantId == _currentTenantId);
        // Repetir para Customer y Payment...

        // 2. Configuración de decimales
        modelBuilder.Entity<Sale>().Property(s => s.TotalAmount).HasPrecision(18, 2);
        modelBuilder.Entity<Sale>().Property(s => s.CostPrice).HasPrecision(18, 2);
        modelBuilder.Entity<Sale>().Property(s => s.CommissionAmount).HasPrecision(18, 2);

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

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // 3. AUDITORÍA AUTOMÁTICA [cite: 16, 53]
        foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.TenantId = _currentTenantId;
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.CreatedBy = _currentUserId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedBy = _currentUserId;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
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