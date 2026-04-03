using BusinessCloud.Domain.Commissions.Entities;
using BusinessCloud.Domain.Common;
using BusinessCloud.Domain.Payments.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BusinessCloud.Infrastructure.Data
{
    public class CommissionsDbContext : DbContext
    {
        private readonly ICurrentUserService _currentUser;

        public CommissionsDbContext(
            DbContextOptions<CommissionsDbContext> options,
            ICurrentUserService currentUser)
            : base(options)
        {
            _currentUser = currentUser;
        }

        // --- Tablas de Commissions ---
        public DbSet<InfluenceCenter> InfluenceCenters => Set<InfluenceCenter>();

       

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // configuración anterior de InfluenceCenter 
            var ic = modelBuilder.Entity<InfluenceCenter>();

            ic.HasKey(x => x.Id);

            ic.Property(x => x.Name)
              .HasMaxLength(200)
              .IsRequired();

            ic.Property(x => x.RFC)
              .HasMaxLength(20)
              .IsRequired();

            ic.Property(x => x.Email)
              .HasMaxLength(200)
              .IsRequired();

            ic.Property(x => x.Username)
              .HasMaxLength(100);

            ic.Property(x => x.PasswordHash)
              .HasMaxLength(500);

            ic.Property(x => x.Role)
              .HasMaxLength(50)
              .IsRequired();

            ic.Property(x => x.CreatedBy)
              .HasMaxLength(100);

            ic.Property(x => x.UpdatedBy)
              .HasMaxLength(100);

            ic.HasIndex(x => x.RFC).IsUnique();
            ic.HasIndex(x => x.Username).IsUnique(false);
            
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

    public class CommissionsDbContextFactory : IDesignTimeDbContextFactory<CommissionsDbContext>
    {
        public CommissionsDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<CommissionsDbContext>();

            optionsBuilder.UseSqlServer(
                "Server=LAPTOP-5L4BL4RK\\SQLEXPRESS;Database=CommissionsDB;Trusted_Connection=True;TrustServerCertificate=True");

            return new CommissionsDbContext(
                optionsBuilder.Options,
                new DummyCurrentUserService());
        }
    }

   
}