using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Catalogs.Entities;
using BusinessCloud.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Infrastructure.Data;

public class CatalogsDbContext : DbContext, ICatalogsDbContext
{
    private readonly ICurrentUserService _userService;

    public CatalogsDbContext(DbContextOptions<CatalogsDbContext> options, ICurrentUserService userService)
        : base(options)
    {
        _userService = userService;
    }

    public DbSet<CatStoreSettings> StoreSettings => Set<CatStoreSettings>();
    public DbSet<CatCategory> Categories => Set<CatCategory>();
    public DbSet<CatProduct> Products => Set<CatProduct>();
    public DbSet<CatProductImage> ProductImages => Set<CatProductImage>();
    public DbSet<CatProductVariant> ProductVariants => Set<CatProductVariant>();
    public DbSet<CatBanner> Banners => Set<CatBanner>();
    public DbSet<CatOrder> Orders => Set<CatOrder>();
    public DbSet<CatOrderItem> OrderItems => Set<CatOrderItem>();
    public DbSet<CatInterestRequest> InterestRequests => Set<CatInterestRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CatStoreSettings>(e =>
        {
            e.ToTable("Cat_StoreSettings");
            e.Property(x => x.TenantId).HasMaxLength(64);
            e.Property(x => x.Slug).IsRequired().HasMaxLength(60);
            e.Property(x => x.PublicName).IsRequired().HasMaxLength(120);
            e.Property(x => x.LogoUrl).HasMaxLength(500);
            e.Property(x => x.LogoBlobName).HasMaxLength(300);
            e.Property(x => x.PrimaryColor).IsRequired().HasMaxLength(7);
            e.Property(x => x.TitleFont).HasConversion<string>().HasMaxLength(32)
                .HasDefaultValue(CatTitleFont.Default);
            e.Property(x => x.Phone).HasMaxLength(30);
            e.Property(x => x.Whatsapp).HasMaxLength(30);
            e.Property(x => x.OrderEmail).HasMaxLength(200);
            e.Property(x => x.Currency).IsRequired().HasMaxLength(3);
            e.Property(x => x.OrderDisclaimer).HasMaxLength(500);
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => x.TenantId).IsUnique();
        });

        modelBuilder.Entity<CatCategory>(e =>
        {
            e.ToTable("Cat_Categories");
            e.Property(x => x.TenantId).HasMaxLength(64);
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.ImageUrl).HasMaxLength(500);
            e.Property(x => x.ImageBlobName).HasMaxLength(300);
            e.HasIndex(x => new { x.TenantId, x.ParentId, x.SortOrder });
        });

        modelBuilder.Entity<CatProduct>(e =>
        {
            e.ToTable("Cat_Products");
            e.Property(x => x.TenantId).HasMaxLength(64);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(4000);
            e.Property(x => x.Price).HasPrecision(18, 2);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Ignore(x => x.TotalStock);
            e.Ignore(x => x.IsSoldOut);
            e.HasIndex(x => new { x.TenantId, x.IsDeleted, x.Status });
            e.HasIndex(x => new { x.TenantId, x.CategoryId });
        });

        modelBuilder.Entity<CatProductImage>(e =>
        {
            e.ToTable("Cat_ProductImages");
            e.Property(x => x.TenantId).HasMaxLength(64);
            e.Property(x => x.Url).IsRequired().HasMaxLength(500);
            e.Property(x => x.BlobName).IsRequired().HasMaxLength(300);
            e.HasOne(x => x.Product).WithMany(p => p.Images)
                .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CatProductVariant>(e =>
        {
            e.ToTable("Cat_ProductVariants");
            e.Property(x => x.TenantId).HasMaxLength(64);
            e.Property(x => x.Size).HasMaxLength(50);
            e.Property(x => x.Color).HasMaxLength(50);
            e.Property(x => x.Sku).HasMaxLength(60);
            e.Ignore(x => x.Label);
            e.HasOne(x => x.Product).WithMany(p => p.Variants)
                .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CatBanner>(e =>
        {
            e.ToTable("Cat_Banners");
            e.Property(x => x.TenantId).HasMaxLength(64);
            e.Property(x => x.ImageUrl).IsRequired().HasMaxLength(500);
            e.Property(x => x.BlobName).IsRequired().HasMaxLength(300);
            e.Property(x => x.Title).HasMaxLength(120);
        });

        modelBuilder.Entity<CatOrder>(e =>
        {
            e.ToTable("Cat_Orders");
            e.Property(x => x.TenantId).HasMaxLength(64);
            e.Property(x => x.OrderNumber).IsRequired().HasMaxLength(20);
            e.Property(x => x.CustomerName).IsRequired().HasMaxLength(120);
            e.Property(x => x.CustomerPhone).IsRequired().HasMaxLength(20);
            e.Property(x => x.Note).HasMaxLength(500);
            e.Property(x => x.InternalNote).HasMaxLength(1000);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.EstimatedTotal).HasPrecision(18, 2);
            e.Property(x => x.FinalTotal).HasPrecision(18, 2);
            e.HasIndex(x => new { x.TenantId, x.OrderNumber }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.Status, x.CreatedAt });
        });

        modelBuilder.Entity<CatOrderItem>(e =>
        {
            e.ToTable("Cat_OrderItems");
            e.Property(x => x.TenantId).HasMaxLength(64);
            e.Property(x => x.ProductName).IsRequired().HasMaxLength(200);
            e.Property(x => x.VariantLabel).HasMaxLength(110);
            e.Property(x => x.ImageUrl).HasMaxLength(500);
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.Property(x => x.LineNote).HasMaxLength(300);
            e.HasOne(x => x.Order).WithMany(o => o.Items)
                .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.VariantId);
        });

        modelBuilder.Entity<CatInterestRequest>(e =>
        {
            e.ToTable("Cat_InterestRequests");
            e.Property(x => x.TenantId).HasMaxLength(64);
            e.Property(x => x.ProductName).IsRequired().HasMaxLength(200);
            e.Property(x => x.VariantLabel).HasMaxLength(110);
            e.Property(x => x.CustomerName).HasMaxLength(120);
            e.Property(x => x.CustomerPhone).IsRequired().HasMaxLength(20);
            e.Property(x => x.Note).HasMaxLength(300);
            e.HasIndex(x => new { x.TenantId, x.IsAttended, x.CreatedAt });
        });

        // Filtros multi-tenant. Las rutas públicas (sin usuario) usan IgnoreQueryFilters y
        // filtran explícitamente por el tenant resuelto desde el slug.
        modelBuilder.Entity<CatStoreSettings>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<CatCategory>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<CatProduct>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<CatProductImage>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<CatProductVariant>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<CatBanner>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<CatOrder>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<CatOrderItem>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<CatInterestRequest>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken)
    {
        if (!Database.IsRelational())
        {
            await operation(cancellationToken);
            return;
        }

        var strategy = Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // Si la transacción se reintenta, se descartan los cambios pendientes del intento anterior.
            ChangeTracker.Clear();
            await using var transaction = await Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, cancellationToken);
            await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                // El TenantId se respeta si ya viene asignado (escrituras del catálogo público).
                if (string.IsNullOrEmpty(entry.Entity.TenantId))
                {
                    entry.Entity.TenantId = _userService.TenantId ?? string.Empty;
                }
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.CreatedBy = _userService.UserId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedBy = _userService.UserId;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
