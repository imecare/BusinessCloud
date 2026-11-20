using BusinessCloud.Application.Common.Interfaces; // <-- ESTO ES LO QUE FALTA PARA QUE ENCUENTRE LA INTERFAZ
using BusinessCloud.Domain.Common;
using BusinessCloud.Domain.Common.Entities;
using BusinessCloud.Domain.Payments.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

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
    public DbSet<PayExpense> Expenses => Set<PayExpense>();
    public DbSet<SaleReservation> Reservations => Set<SaleReservation>();
    public DbSet<Seller> Sellers => Set<Seller>();
    public DbSet<DeletedPayment> DeletedPayments => Set<DeletedPayment>();
    public DbSet<DeletedSale> DeletedSales => Set<DeletedSale>();
    public DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Excluir entidades que ya maneja IdentityDbContext
        modelBuilder.Entity<Tenant>().ToTable("Tenants", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<TenantModule>().ToTable("TenantModules", t => t.ExcludeFromMigrations());

        // 1. FILTRO GLOBAL SAAS: Seguridad automática
        // IMPORTANTE: Aquí usamos la propiedad del servicio
        modelBuilder.Entity<Sale>().HasQueryFilter(s => s.TenantId == _userService.TenantId);
        modelBuilder.Entity<Payment>().HasQueryFilter(p => p.TenantId == _userService.TenantId);
        modelBuilder.Entity<Customer>().HasQueryFilter(c => c.TenantId == _userService.TenantId);
        modelBuilder.Entity<Seller>().HasQueryFilter(c => c.TenantId == _userService.TenantId);
        modelBuilder.Entity<PayExpense>().HasQueryFilter(e => e.TenantId == _userService.TenantId);
        modelBuilder.Entity<SaleReservation>().HasQueryFilter(r => r.TenantId == _userService.TenantId);

        // 2. Configuración de precisión
        modelBuilder.Entity<Sale>().Property(s => s.TotalAmount).HasPrecision(18, 2);
        modelBuilder.Entity<Sale>().Property(s => s.CostPrice).HasPrecision(18, 2);
        modelBuilder.Entity<Sale>().Property(s => s.CommissionAmount).HasPrecision(18, 2);
        modelBuilder.Entity<Payment>().Property(e => e.Amount).HasPrecision(18, 2);
        modelBuilder.Entity<PayExpense>().Property(e => e.Cost).HasPrecision(18, 2);
        modelBuilder.Entity<PayExpense>().Property(e => e.Description).HasMaxLength(500);
        modelBuilder.Entity<PayExpense>().Property(e => e.PaymentType).HasMaxLength(20);

        modelBuilder.Entity<SaleReservation>().Property(r => r.TotalAmount).HasPrecision(18, 2);
        modelBuilder.Entity<SaleReservation>().Property(r => r.CostPrice).HasPrecision(18, 2);
        modelBuilder.Entity<SaleReservation>().Property(r => r.CommissionAmount).HasPrecision(18, 2);
        modelBuilder.Entity<SaleReservation>().Property(r => r.ProductDescription).HasMaxLength(500);
        modelBuilder.Entity<DeletedPayment>().Property(e => e.Amount).HasPrecision(18, 2);
        modelBuilder.Entity<DeletedSale>().Property(e => e.TotalAmount).HasPrecision(18, 2);
        modelBuilder.Entity<DeletedSale>().Property(e => e.CostPrice).HasPrecision(18, 2);
        modelBuilder.Entity<DeletedSale>().Property(e => e.CommissionAmount).HasPrecision(18, 2);

        // 3. Campos de auditoría de comisión
        modelBuilder.Entity<Sale>().Property(s => s.CommissionPaymentNote).HasMaxLength(500);
        modelBuilder.Entity<Sale>().Property(s => s.CommissionPaidByUserId).HasMaxLength(450);

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasIndex(e => new { e.RFC, e.Phone });
            entity.HasOne(e => e.Seller).WithMany(s => s.Customers)
                .HasForeignKey(e => e.SellerId).OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.TenantId).HasMaxLength(450);
            entity.HasIndex(e => e.TenantId).IsUnique()
                .HasFilter("[IsGeneralPublic] = 1");
        });
        modelBuilder.Entity<Sale>().Property(s => s.TenantId).HasMaxLength(450);
        modelBuilder.Entity<Sale>().Property(s => s.IdempotencyKey).HasMaxLength(100);
        modelBuilder.Entity<Sale>().HasIndex(s => new { s.TenantId, s.IdempotencyKey })
            .IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL");
    }

    public async Task<T> ExecuteSaleTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        var tenantId = _userService.GetRequiredTenantId();
        if (!Database.IsRelational())
            return await operation(cancellationToken);

        var attempt = 0;
        return await Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            if (attempt++ > 0)
                ChangeTracker.Clear();

            await using var transaction = await Database.BeginTransactionAsync(cancellationToken);
            // Serialize creation per tenant, including retries after an uncertain commit.
            var resource = $"payments-create-sale:{tenantId}";
            await Database.ExecuteSqlInterpolatedAsync($@"
                DECLARE @result int;
                EXEC @result = sys.sp_getapplock @Resource={resource},
                    @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000;
                IF @result < 0 THROW 51000, 'No se pudo bloquear el registro de la venta. Reintente.', 1;",
                cancellationToken);
            var result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // 3. AUDITORÍA E INYECCIÓN DE TENANT AUTOMÁTICA
        foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.TenantId = _userService.TenantId ?? string.Empty;
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
        var configuration = IdentityDbContextFactory.BuildConfiguration();

        var optionsBuilder = new DbContextOptionsBuilder<PaymentsDbContext>();
        optionsBuilder.UseSqlServer(configuration.GetConnectionString("PaymentsConnection"));

        return new PaymentsDbContext(optionsBuilder.Options, new DummyCurrentUserService());
    }
}