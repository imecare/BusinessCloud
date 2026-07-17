using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Common.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Infrastructure.Data
{
    public class IdentityDbContext : IdentityDbContext<ApplicationUser>, IIdentityDbContext
    {
        public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<TenantModule> TenantModules => Set<TenantModule>();
        public DbSet<TenantSubscription> TenantSubscriptions => Set<TenantSubscription>();
        public DbSet<SystemSeller> SystemSellers => Set<SystemSeller>();
        public DbSet<SellerCommission> SellerCommissions => Set<SellerCommission>();
        public DbSet<Package> Packages => Set<Package>();
        public DbSet<PackagePurchase> PackagePurchases => Set<PackagePurchase>();
        public DbSet<TenantMessageBalance> TenantMessageBalances => Set<TenantMessageBalance>();
        public DbSet<PlatformSettings> PlatformSettings => Set<PlatformSettings>();
        public DbSet<MessagePackageRequest> MessagePackageRequests => Set<MessagePackageRequest>();
        public DbSet<ContactRequest> ContactRequests => Set<ContactRequest>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // ¡Vital para Identity!

            builder.Entity<Tenant>().HasKey(t => t.Id);

            builder.Entity<TenantModule>(e =>
            {
                e.HasKey(tm => tm.Id);
                e.HasIndex(tm => new { tm.TenantId, tm.Module }).IsUnique();
                e.HasOne(tm => tm.Tenant)
                    .WithMany(t => t.Modules)
                    .HasForeignKey(tm => tm.TenantId);
            });

            builder.Entity<TenantSubscription>(e =>
            {
                e.HasKey(s => s.Id);
                e.HasIndex(s => s.TenantId).IsUnique();
                e.HasOne(s => s.Tenant)
                    .WithOne()
                    .HasForeignKey<TenantSubscription>(s => s.TenantId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.Property(s => s.Price).HasColumnType("decimal(18,2)");
                e.Property(s => s.PlanName).HasMaxLength(100);
                e.Property(s => s.Currency).HasMaxLength(3);
                e.Property(s => s.OwnerName).HasMaxLength(200);
                e.Property(s => s.OwnerPhone).HasMaxLength(20);
                e.Property(s => s.Notes).HasMaxLength(1000);
                e.Property(s => s.Period).HasConversion<int>();
                e.Property(s => s.CommissionInitialAmount).HasColumnType("decimal(18,2)");
                e.Property(s => s.CommissionMonthlyPercent).HasColumnType("decimal(5,2)");
                e.Ignore(s => s.GraceEndsOn);
            });

            builder.Entity<SystemSeller>(e =>
            {
                e.HasKey(s => s.Id);
                e.Property(s => s.Name).HasMaxLength(200).IsRequired();
                e.Property(s => s.Email).HasMaxLength(256);
                e.Property(s => s.Phone).HasMaxLength(20);
                e.Property(s => s.DefaultInitialAmount).HasColumnType("decimal(18,2)");
                e.Property(s => s.DefaultMonthlyPercent).HasColumnType("decimal(5,2)");
            });

            builder.Entity<SellerCommission>(e =>
            {
                e.HasKey(c => c.Id);
                e.HasIndex(c => new { c.SystemSellerId, c.IsPaid });
                e.HasOne(c => c.Seller)
                    .WithMany(s => s.Commissions)
                    .HasForeignKey(c => c.SystemSellerId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.Property(c => c.Type).HasConversion<int>();
                e.Property(c => c.BaseAmount).HasColumnType("decimal(18,2)");
                e.Property(c => c.Percent).HasColumnType("decimal(5,2)");
                e.Property(c => c.Amount).HasColumnType("decimal(18,2)");
                e.Property(c => c.Notes).HasMaxLength(500);
            });

            builder.Entity<Package>(e =>
            {
                e.HasKey(p => p.Id);
                e.Property(p => p.Name).HasMaxLength(150).IsRequired();
                e.Property(p => p.Module).HasMaxLength(50);
                e.Property(p => p.Currency).HasMaxLength(3);
                e.Property(p => p.Price).HasColumnType("decimal(18,2)");
                e.Property(p => p.Description).HasMaxLength(500);
            });

            builder.Entity<PackagePurchase>(e =>
            {
                e.HasKey(p => p.Id);
                e.HasIndex(p => p.TenantId);
                e.Property(p => p.PackageName).HasMaxLength(150);
                e.Property(p => p.Price).HasColumnType("decimal(18,2)");
                e.Property(p => p.Note).HasMaxLength(500);
                e.HasOne(p => p.Package)
                    .WithMany()
                    .HasForeignKey(p => p.PackageId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<TenantMessageBalance>(e =>
            {
                e.HasKey(b => b.Id);
                e.HasIndex(b => b.TenantId).IsUnique();
                e.HasOne(b => b.Tenant)
                    .WithOne()
                    .HasForeignKey<TenantMessageBalance>(b => b.TenantId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<PlatformSettings>(e =>
            {
                e.HasKey(p => p.Id);
                e.Property(p => p.SuperAdminPhone).HasMaxLength(20);
            });

            builder.Entity<MessagePackageRequest>(e =>
            {
                e.HasKey(r => r.Id);
                e.HasIndex(r => new { r.Status, r.RequestedAt });
                e.Property(r => r.CompanyName).HasMaxLength(200);
                e.Property(r => r.PackageName).HasMaxLength(150);
                e.Property(r => r.Status).HasMaxLength(20);
                e.Property(r => r.RequestedByName).HasMaxLength(200);
                e.Property(r => r.Note).HasMaxLength(500);
                e.Property(r => r.Price).HasColumnType("decimal(18,2)");
            });

            builder.Entity<ContactRequest>(e =>
            {
                e.HasKey(r => r.Id);
                e.HasIndex(r => new { r.Status, r.CreatedAt });
                e.Property(r => r.Phone).HasMaxLength(20);
                e.Property(r => r.Type).HasMaxLength(20);
                e.Property(r => r.Status).HasMaxLength(20);
                e.Property(r => r.Message).HasMaxLength(500);
            });
        }
    }
}
