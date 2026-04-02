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

        // --- Tablas de Payments (Abonos) ---
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Sale> Sales => Set<Sale>();
        public DbSet<Payment> Payments => Set<Payment>();


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

    public class DummyCurrentUserService : ICurrentUserService
    {
        public string? UserId => "SYSTEM";
        public string? Username => "SYSTEM";
        public string? Role => "SYSTEM";
    }
}