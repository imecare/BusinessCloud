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
    public DbSet<BzaEvent> Events => Set<BzaEvent>();
    public DbSet<BzaSale> Sales => Set<BzaSale>();
    public DbSet<BzaSoldProduct> SoldProducts => Set<BzaSoldProduct>();
    public DbSet<BzaPayment> Payments => Set<BzaPayment>();
    public DbSet<BzaDispatchSheet> DispatchSheets => Set<BzaDispatchSheet>();
    public DbSet<BzaDispatchItem> DispatchItems => Set<BzaDispatchItem>();
    public DbSet<BzaDelivery> Deliveries => Set<BzaDelivery>();
    public DbSet<BzaDeliveryItem> DeliveryItems => Set<BzaDeliveryItem>();
    public DbSet<BzaNotificationSettings> NotificationSettings => Set<BzaNotificationSettings>();
    public DbSet<BzaPaymentCard> PaymentCards => Set<BzaPaymentCard>();
    public DbSet<BzaBazarSettings> BazarSettings => Set<BzaBazarSettings>();
    public DbSet<BzaContactPhone> ContactPhones => Set<BzaContactPhone>();
    public DbSet<BzaFacebookProfile> FacebookProfiles => Set<BzaFacebookProfile>();
    public DbSet<BzaClosureEvent> ClosureEvents => Set<BzaClosureEvent>();
    public DbSet<BzaClosureEventItem> ClosureEventItems => Set<BzaClosureEventItem>();
    public DbSet<BzaClosureGroupDelivery> ClosureGroupDeliveries => Set<BzaClosureGroupDelivery>();
    public DbSet<BzaClosureCustomerTotal> ClosureCustomerTotals => Set<BzaClosureCustomerTotal>();
    public DbSet<BzaClosureProof> ClosureProofs => Set<BzaClosureProof>();
    public DbSet<BzaProofRejection> ProofRejections => Set<BzaProofRejection>();
    public DbSet<BzaSaleCancellation> SaleCancellations => Set<BzaSaleCancellation>();
    public DbSet<BzaBlockedCustomer> BlockedCustomers => Set<BzaBlockedCustomer>();
    public DbSet<BzaWhatsAppMessage> WhatsAppMessages => Set<BzaWhatsAppMessage>();
    public DbSet<BzaCustomerNotificationSubscription> CustomerNotificationSubscriptions => Set<BzaCustomerNotificationSubscription>();
    public DbSet<BzaNotificationLog> NotificationLogs => Set<BzaNotificationLog>();

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
        modelBuilder.Entity<BzaEvent>().ToTable("Bza_Events");
        modelBuilder.Entity<BzaSale>().ToTable("Bza_Sales");
        modelBuilder.Entity<BzaSoldProduct>().ToTable("Bza_SoldProducts");
        modelBuilder.Entity<BzaPayment>().ToTable("Bza_Payments");
        modelBuilder.Entity<BzaDispatchSheet>().ToTable("Bza_DispatchSheets");
        modelBuilder.Entity<BzaDispatchItem>().ToTable("Bza_DispatchItems");
        modelBuilder.Entity<BzaDelivery>().ToTable("Bza_Deliveries");
        modelBuilder.Entity<BzaDeliveryItem>().ToTable("Bza_DeliveryItems");
        modelBuilder.Entity<BzaNotificationSettings>().ToTable("Bza_NotificationSettings");
        modelBuilder.Entity<BzaPaymentCard>().ToTable("Bza_PaymentCards");
        modelBuilder.Entity<BzaBazarSettings>().ToTable("Bza_BazarSettings");
        modelBuilder.Entity<BzaContactPhone>().ToTable("Bza_ContactPhones");
        modelBuilder.Entity<BzaFacebookProfile>().ToTable("Bza_FacebookProfiles");
        modelBuilder.Entity<BzaClosureEvent>().ToTable("Bza_ClosureEvents");
        modelBuilder.Entity<BzaClosureEventItem>().ToTable("Bza_ClosureEventItems");
        modelBuilder.Entity<BzaClosureGroupDelivery>().ToTable("Bza_ClosureGroupDeliveries");
        modelBuilder.Entity<BzaClosureCustomerTotal>().ToTable("Bza_ClosureCustomerTotals");
        modelBuilder.Entity<BzaClosureProof>().ToTable("Bza_ClosureProofs");
        modelBuilder.Entity<BzaProofRejection>().ToTable("Bza_ProofRejections");
        modelBuilder.Entity<BzaSaleCancellation>().ToTable("Bza_SaleCancellations");
        modelBuilder.Entity<BzaBlockedCustomer>().ToTable("Bza_BlockedCustomers");
        modelBuilder.Entity<BzaWhatsAppMessage>().ToTable("Bza_WhatsAppMessages");
        modelBuilder.Entity<BzaCustomerNotificationSubscription>().ToTable("Bza_CustomerNotificationSubscriptions");
        modelBuilder.Entity<BzaNotificationLog>().ToTable("Bza_NotificationLogs");
        modelBuilder.Entity<BzaWhatsAppMessage>().HasIndex(m => m.WaMessageId);
        modelBuilder.Entity<BzaCustomerNotificationSubscription>().HasIndex(x => new { x.TenantId, x.BzaCustomerId, x.Endpoint }).IsUnique();
        modelBuilder.Entity<BzaNotificationLog>().HasIndex(x => new { x.TenantId, x.BzaClosureEventId, x.BzaClosureCustomerTotalId, x.SentAt });

        // ─────────────────────────────────────────────────────────────────────
        // Precisión de decimales
        // ─────────────────────────────────────────────────────────────────────
        modelBuilder.Entity<BzaPayment>().Property(p => p.Amount).HasPrecision(18, 2);
        modelBuilder.Entity<BzaSoldProduct>().Property(p => p.Price).HasPrecision(18, 2);
        modelBuilder.Entity<BzaClosureCustomerTotal>().Property(p => p.TotalAmount).HasPrecision(18, 2);
        modelBuilder.Entity<BzaProofRejection>().Property(p => p.TotalAmount).HasPrecision(18, 2);
        modelBuilder.Entity<BzaSaleCancellation>().Property(p => p.TotalAmount).HasPrecision(18, 2);

        // ─────────────────────────────────────────────────────────────────────────────
        // Relaciones de BzaSoldProduct (Producto perteneciente a una Venta)
        // ─────────────────────────────────────────────────────────────────────────────
        modelBuilder.Entity<BzaSoldProduct>()
            .HasOne(p => p.Sale)
            .WithMany(s => s.Products)
            .HasForeignKey(p => p.BzaSaleId)
            .OnDelete(DeleteBehavior.Cascade);

        // ─────────────────────────────────────────────────────────────────────────────
        // Relaciones de BzaSale (Venta = Cliente + Evento, con N productos)
        // ─────────────────────────────────────────────────────────────────────────────
        modelBuilder.Entity<BzaSale>()
            .HasOne(s => s.Event)
            .WithMany(e => e.Sales)
            .HasForeignKey(s => s.BzaEventId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BzaSale>()
            .HasOne(s => s.Customer)
            .WithMany(c => c.Sales)
            .HasForeignKey(s => s.BzaCustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Origen de la venta (1 = Captura directa, 2 = Excel). Por defecto: captura directa.
        modelBuilder.Entity<BzaSale>()
            .Property(s => s.Source)
            .HasDefaultValue(1);

        // Vínculo opcional Venta -> Evento de Cierre (Envío de Totales).
        // Una venta solo puede estar en un evento de pago. Si el cierre se elimina,
        // la venta se libera (SetNull) para poder volver a enviarse.
        modelBuilder.Entity<BzaSale>()
            .HasOne<BzaClosureEvent>()
            .WithMany()
            .HasForeignKey(s => s.BzaClosureEventId)
            .OnDelete(DeleteBehavior.SetNull);

        // ─────────────────────────────────────────────────────────────────────
        // Relaciones de BzaPayment (Pago de Cliente en un Evento de Venta)
        // ─────────────────────────────────────────────────────────────────────
        modelBuilder.Entity<BzaPayment>()
            .HasOne(p => p.Event)
            .WithMany(e => e.Payments)
            .HasForeignKey(p => p.BzaEventId)
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
            .HasOne(d => d.Event)
            .WithMany()
            .HasForeignKey(d => d.BzaEventId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BzaDeliveryItem>()
            .HasOne(d => d.Event)
            .WithMany()
            .HasForeignKey(d => d.BzaEventId)
            .OnDelete(DeleteBehavior.Restrict);

        // ─────────────────────────────────────────────────────────────────────
        // Relaciones del Cierre de Venta (Envío de Totales)
        // ─────────────────────────────────────────────────────────────────────
        modelBuilder.Entity<BzaClosureEventItem>()
            .HasOne(i => i.ClosureEvent)
            .WithMany(c => c.Items)
            .HasForeignKey(i => i.BzaClosureEventId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BzaClosureEventItem>()
            .HasOne(i => i.Event)
            .WithMany()
            .HasForeignKey(i => i.BzaEventId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BzaClosureGroupDelivery>()
            .HasOne(g => g.ClosureEvent)
            .WithMany(c => c.GroupDeliveries)
            .HasForeignKey(g => g.BzaClosureEventId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BzaClosureGroupDelivery>()
            .HasOne(g => g.CollectorGroup)
            .WithMany()
            .HasForeignKey(g => g.BzaCollectorGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BzaClosureCustomerTotal>()
            .HasOne(t => t.ClosureEvent)
            .WithMany(c => c.CustomerTotals)
            .HasForeignKey(t => t.BzaClosureEventId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BzaClosureCustomerTotal>()
            .HasOne(t => t.Customer)
            .WithMany()
            .HasForeignKey(t => t.BzaCustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BzaClosureCustomerTotal>()
            .HasIndex(t => t.UploadToken)
            .IsUnique();

        modelBuilder.Entity<BzaClosureProof>()
            .HasOne(p => p.Total)
            .WithMany(t => t.Proofs)
            .HasForeignKey(p => p.BzaClosureCustomerTotalId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BzaCustomerNotificationSubscription>()
            .HasOne(s => s.Customer)
            .WithMany()
            .HasForeignKey(s => s.BzaCustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BzaCustomerNotificationSubscription>()
            .HasOne(s => s.ClosureCustomerTotal)
            .WithMany()
            .HasForeignKey(s => s.BzaClosureCustomerTotalId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<BzaCustomerNotificationSubscription>()
            .Property(s => s.Endpoint)
            .HasMaxLength(1000);

        modelBuilder.Entity<BzaCustomerNotificationSubscription>()
            .Property(s => s.P256dh)
            .HasMaxLength(300);

        modelBuilder.Entity<BzaCustomerNotificationSubscription>()
            .Property(s => s.Auth)
            .HasMaxLength(200);

        modelBuilder.Entity<BzaNotificationLog>()
            .HasOne(l => l.ClosureEvent)
            .WithMany()
            .HasForeignKey(l => l.BzaClosureEventId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BzaNotificationLog>()
            .HasOne(l => l.ClosureCustomerTotal)
            .WithMany()
            .HasForeignKey(l => l.BzaClosureCustomerTotalId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BzaNotificationLog>()
            .HasOne(l => l.Customer)
            .WithMany()
            .HasForeignKey(l => l.BzaCustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BzaNotificationLog>()
            .Property(l => l.ErrorMessage)
            .HasMaxLength(500);

        modelBuilder.Entity<BzaClosureCustomerTotal>()
            .Property(t => t.RejectionReason)
            .HasMaxLength(500);

        modelBuilder.Entity<BzaClosureCustomerTotal>()
            .Property(t => t.CustomerJustification)
            .HasMaxLength(500);

        // ───────────────────────────────────────────────
        // El teléfono es la llave del cliente para el envío de totales: único por tenant.
        // ───────────────────────────────────────────────
        modelBuilder.Entity<BzaCustomer>()
            .HasIndex(c => new { c.TenantId, c.Phone })
            .IsUnique()
            .HasDatabaseName("UX_Bza_Customers_TenantId_Phone")
            .HasFilter("[Phone] IS NOT NULL AND [Phone] <> ''");

        // ───────────────────────────────────────────────
        // Configuración general del bazar (identidad, contacto y redes)
        // ───────────────────────────────────────────────
        modelBuilder.Entity<BzaBazarSettings>().Property(s => s.BazarName).HasMaxLength(150);
        modelBuilder.Entity<BzaBazarSettings>().Property(s => s.LogoUrl).HasMaxLength(500);
        modelBuilder.Entity<BzaBazarSettings>().Property(s => s.PhysicalAddress).HasMaxLength(300);
        modelBuilder.Entity<BzaBazarSettings>().Property(s => s.FacebookPageUrl).HasMaxLength(300);

        modelBuilder.Entity<BzaContactPhone>().Property(p => p.PhoneNumber).HasMaxLength(30);
        modelBuilder.Entity<BzaContactPhone>().Property(p => p.Label).HasMaxLength(80);

        modelBuilder.Entity<BzaFacebookProfile>().Property(p => p.Name).HasMaxLength(120);
        modelBuilder.Entity<BzaFacebookProfile>().Property(p => p.ProfileUrl).HasMaxLength(300);

        modelBuilder.Entity<BzaContactPhone>()
            .HasOne(p => p.BazarSettings)
            .WithMany(s => s.ContactPhones)
            .HasForeignKey(p => p.BzaBazarSettingsId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BzaFacebookProfile>()
            .HasOne(p => p.BazarSettings)
            .WithMany(s => s.FacebookProfiles)
            .HasForeignKey(p => p.BzaBazarSettingsId)
            .OnDelete(DeleteBehavior.Cascade);

        // ─────────────────────────────────────────────────────────────────────
        // Multi-tenant Query Filters
        // ─────────────────────────────────────────────────────────────────────
        modelBuilder.Entity<BzaCollectorGroup>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaCollector>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaCustomer>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaDate>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaEvent>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaSale>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaSoldProduct>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaPayment>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaDispatchSheet>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaDispatchItem>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaDelivery>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaDeliveryItem>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaNotificationSettings>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaPaymentCard>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaBazarSettings>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaContactPhone>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaFacebookProfile>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaClosureEvent>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaClosureEventItem>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaClosureGroupDelivery>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaClosureCustomerTotal>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaClosureProof>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaProofRejection>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaSaleCancellation>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaCustomerNotificationSubscription>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
        modelBuilder.Entity<BzaNotificationLog>().HasQueryFilter(x => x.TenantId == _userService.TenantId);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                // No sobrescribir el TenantId si ya fue asignado explícitamente
                // (necesario para escrituras desde el portal público por token).
                if (string.IsNullOrEmpty(entry.Entity.TenantId))
                {
                    entry.Entity.TenantId = _userService.TenantId ?? "";
                }
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.CreatedBy = _userService.UserId;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}