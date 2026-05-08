using BusinessCloud.Domain.Common.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Infrastructure.Data
{
    public class IdentityDbContext : IdentityDbContext<ApplicationUser>
    {
        public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<TenantModule> TenantModules => Set<TenantModule>();

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
        }
    }
}
