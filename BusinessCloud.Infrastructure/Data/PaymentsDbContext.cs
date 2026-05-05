using BusinessCloud.Application.Common.Interfaces; // <-- ESTO ES LO QUE FALTA PARA QUE ENCUENTRE LA INTERFAZ
using BusinessCloud.Domain.Common;
using BusinessCloud.Domain.Common.Entities;
using BusinessCloud.Domain.Payments.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BusinessCloud.Infrastructure.Data;

public class PaymentsDbContext : DbContext, IPaymentsDbContext
{
    // Limpiamos las variables redundantes para usar solo el servicio
    private readonly ICurrentUserService _userService;

    public PaymentsDbContext(
        DbContextOptions<PaymentsDbContext> options,
        ICurrentUserService userService) : base(options)
    {
        _userService = userService;
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Seller> Sellers => Set<Seller>();
    public DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. FILTRO GLOBAL SAAS: Seguridad automática
        // IMPORTANTE: Aquí usamos la propiedad del servicio
        modelBuilder.Entity<Sale>().HasQueryFilter(s => s.TenantId == _userService.TenantId);
        modelBuilder.Entity<Payment>().HasQueryFilter(p => p.TenantId == _userService.TenantId);
        modelBuilder.Entity<Customer>().HasQueryFilter(c => c.TenantId == _userService.TenantId);
        modelBuilder.Entity<Seller>().HasQueryFilter(c => c.TenantId == _userService.TenantId);

        // 2. Configuración de precisión
        modelBuilder.Entity<Sale>().Property(s => s.TotalAmount).HasPrecision(18, 2);
        modelBuilder.Entity<Sale>().Property(s => s.CostPrice).HasPrecision(18, 2);
        modelBuilder.Entity<Sale>().Property(s => s.CommissionAmount).HasPrecision(18, 2);
        modelBuilder.Entity<Payment>().Property(e => e.Amount).HasPrecision(18, 2);

        // 3. Campos de auditoría de comisión
        modelBuilder.Entity<Sale>().Property(s => s.CommissionPaymentNote).HasMaxLength(500);
        modelBuilder.Entity<Sale>().Property(s => s.CommissionPaidByUserId).HasMaxLength(450);

        modelBuilder.Entity<Customer>(entity => {
            entity.HasIndex(e => new { e.RFC, e.Phone });
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // 3. AUDITORÍA E INYECCIÓN DE TENANT AUTOMÁTICA
        foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.TenantId = _userService.TenantId;
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.CreatedBy = _userService.UserId ?? "System";
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedBy = _userService.UserId ?? "System";
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
        optionsBuilder.UseSqlServer("Server=LAPTOP-5L4BL4RK\\SQLEXPRESS;Database=Payments;Trusted_Connection=True;TrustServerCertificate=True");

        return new PaymentsDbContext(optionsBuilder.Options, new DummyCurrentUserService());
    }
}