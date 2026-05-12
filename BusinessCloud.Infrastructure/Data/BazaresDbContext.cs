using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using BusinessCloud.Domain.Common;
using BusinessCloud.Domain.Common.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Infrastructure.Data;

public class BazaresDbContext : DbContext, IBazaresDbContext
{
    private readonly ICurrentUserService _userService;

    public BazaresDbContext(DbContextOptions<BazaresDbContext> options, ICurrentUserService userService)
        : base(options)
    {
        _userService = userService;
    }

    public DbSet<BzaCollector> Collectors => Set<BzaCollector>();
    public DbSet<BzaCustomer> Customers => Set<BzaCustomer>();
    public DbSet<BzaDate> Dates => Set<BzaDate>();
    public DbSet<BzaSale> Sales => Set<BzaSale>();
    public DbSet<BzaProduct> Products => Set<BzaProduct>();
    public DbSet<BzaPayment> Payments => Set<BzaPayment>();
    public DbSet<BzaDispatchSheet> DispatchSheets => Set<BzaDispatchSheet>();
    public DbSet<BzaDispatchItem> DispatchItems => Set<BzaDispatchItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Prefijos Bza_
        modelBuilder.Entity<BzaCollector>().ToTable("Bza_Collectors");
        modelBuilder.Entity<BzaCustomer>().ToTable("Bza_Customers");
        modelBuilder.Entity<BzaDate>().ToTable("Bza_Dates");
        modelBuilder.Entity<BzaSale>().ToTable("Bza_Sales");
        modelBuilder.Entity<BzaProduct>().ToTable("Bza_Products");
        modelBuilder.Entity<BzaPayment>().ToTable("Bza_Payments");
        modelBuilder.Entity<BzaDispatchSheet>().ToTable("Bza_DispatchSheets");
        modelBuilder.Entity<BzaDispatchItem>().ToTable("Bza_DispatchItems");

        // Precisión de decimales
        modelBuilder.Entity<BzaPayment>().Property(p => p.Amount).HasPrecision(18, 2);
        modelBuilder.Entity<BzaProduct>().Property(p => p.Price).HasPrecision(18, 2);
        modelBuilder.Entity<BzaSale>().Property(p => p.Total).HasPrecision(18, 2);

        // Evitar cascade cycles en DispatchItems
        modelBuilder.Entity<BzaDispatchItem>()
            .HasOne(d => d.Sale)
            .WithMany()
            .HasForeignKey(d => d.BzaSaleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Multi-tenant Filter
        modelBuilder.Entity<BzaCollector>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaCustomer>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaDate>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaSale>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaProduct>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaPayment>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaDispatchSheet>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaDispatchItem>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.TenantId = _userService.TenantId ?? "";
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.CreatedBy = _userService.UserId;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}