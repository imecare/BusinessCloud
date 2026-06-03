using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using BusinessCloud.Domain.Common;
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

    public DbSet<BzaCollectorGroup> CollectorGroups => Set<BzaCollectorGroup>();
    public DbSet<BzaCollector> Collectors => Set<BzaCollector>();
    public DbSet<BzaCustomer> Customers => Set<BzaCustomer>();
    public DbSet<BzaDate> Dates => Set<BzaDate>();
    public DbSet<BzaSale> Sales => Set<BzaSale>();
    public DbSet<BzaSoldProduct> SoldProducts => Set<BzaSoldProduct>();
    public DbSet<BzaPayment> Payments => Set<BzaPayment>();
    public DbSet<BzaDispatchSheet> DispatchSheets => Set<BzaDispatchSheet>();
    public DbSet<BzaDispatchItem> DispatchItems => Set<BzaDispatchItem>();
    public DbSet<BzaDelivery> Deliveries => Set<BzaDelivery>();
    public DbSet<BzaDeliveryItem> DeliveryItems => Set<BzaDeliveryItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ─────────────────────────────────────────────────────────────────────
        // Nombres de tablas con prefijo Bza_
        // ─────────────────────────────────────────────────────────────────────
        modelBuilder.Entity<BzaCollectorGroup>().ToTable("Bza_CollectorGroups");
        modelBuilder.Entity<BzaCollector>().ToTable("Bza_Collectors");
        modelBuilder.Entity<BzaCustomer>().ToTable("Bza_Customers");
        modelBuilder.Entity<BzaDate>().ToTable("Bza_Dates");
        modelBuilder.Entity<BzaSale>().ToTable("Bza_Sales");
        modelBuilder.Entity<BzaSoldProduct>().ToTable("Bza_SoldProducts");
        modelBuilder.Entity<BzaPayment>().ToTable("Bza_Payments");
        modelBuilder.Entity<BzaDispatchSheet>().ToTable("Bza_DispatchSheets");
        modelBuilder.Entity<BzaDispatchItem>().ToTable("Bza_DispatchItems");
        modelBuilder.Entity<BzaDelivery>().ToTable("Bza_Deliveries");
        modelBuilder.Entity<BzaDeliveryItem>().ToTable("Bza_DeliveryItems");

        // ─────────────────────────────────────────────────────────────────────
        // Precisión de decimales
        // ─────────────────────────────────────────────────────────────────────
        modelBuilder.Entity<BzaPayment>().Property(p => p.Amount).HasPrecision(18, 2);
        modelBuilder.Entity<BzaSoldProduct>().Property(p => p.Price).HasPrecision(18, 2);

        // ─────────────────────────────────────────────────────────────────────────────
        // Relaciones de BzaSoldProduct (Producto vendido a Cliente en un Evento de Venta)
        // ─────────────────────────────────────────────────────────────────────────────
        modelBuilder.Entity<BzaSoldProduct>()
            .HasOne(p => p.Sale)
            .WithMany(s => s.SoldProducts)
            .HasForeignKey(p => p.BzaSaleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BzaSoldProduct>()
            .HasOne(p => p.Customer)
            .WithMany(c => c.SoldProducts)
            .HasForeignKey(p => p.BzaCustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // ─────────────────────────────────────────────────────────────────────
        // Relaciones de BzaPayment (Pago de Cliente en un Evento de Venta)
        // ─────────────────────────────────────────────────────────────────────
        modelBuilder.Entity<BzaPayment>()
            .HasOne(p => p.Sale)
            .WithMany(s => s.Payments)
            .HasForeignKey(p => p.BzaSaleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BzaPayment>()
            .HasOne(p => p.Customer)
            .WithMany(c => c.Payments)
            .HasForeignKey(p => p.BzaCustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // ─────────────────────────────────────────────────────────────────────
        // Relaciones de BzaDispatchItem (Evitar cascade cycles)
        // ─────────────────────────────────────────────────────────────────────
        modelBuilder.Entity<BzaDispatchItem>()
            .HasOne(d => d.Sale)
            .WithMany()
            .HasForeignKey(d => d.BzaSaleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BzaDeliveryItem>()
            .HasOne(d => d.Sale)
            .WithMany()
            .HasForeignKey(d => d.BzaSaleId)
            .OnDelete(DeleteBehavior.Restrict);

        // ─────────────────────────────────────────────────────────────────────
        // Multi-tenant Query Filters
        // ─────────────────────────────────────────────────────────────────────
        modelBuilder.Entity<BzaCollectorGroup>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaCollector>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaCustomer>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaDate>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaSale>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaSoldProduct>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaPayment>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaDispatchSheet>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaDispatchItem>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaDelivery>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaDeliveryItem>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
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