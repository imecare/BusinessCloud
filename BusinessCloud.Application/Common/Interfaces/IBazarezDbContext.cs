using BusinessCloud.Domain.Bazares.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Common.Interfaces;

public interface IBazaresDbContext
{
    DbSet<BzaCollectorGroup> CollectorGroups { get; }
    DbSet<BzaCollector> Collectors { get; }
    DbSet<BzaCustomer> Customers { get; }
    DbSet<BzaDate> Dates { get; }
    DbSet<BzaEvent> Events { get; }
    DbSet<BzaSale> Sales { get; }
    DbSet<BzaSoldProduct> SoldProducts { get; }
    DbSet<BzaPayment> Payments { get; }
    DbSet<BzaDispatchSheet> DispatchSheets { get; }
    DbSet<BzaDispatchItem> DispatchItems { get; }
    DbSet<BzaDelivery> Deliveries { get; }
    DbSet<BzaDeliveryItem> DeliveryItems { get; }
    DbSet<BzaNotificationSettings> NotificationSettings { get; }
    DbSet<BzaPaymentCard> PaymentCards { get; }
    DbSet<BzaBazarSettings> BazarSettings { get; }
    DbSet<BzaContactPhone> ContactPhones { get; }
    DbSet<BzaFacebookProfile> FacebookProfiles { get; }
    DbSet<BzaClosureEvent> ClosureEvents { get; }
    DbSet<BzaClosureEventItem> ClosureEventItems { get; }
    DbSet<BzaClosureGroupDelivery> ClosureGroupDeliveries { get; }
    DbSet<BzaClosureCustomerTotal> ClosureCustomerTotals { get; }
    DbSet<BzaClosureProof> ClosureProofs { get; }
    DbSet<BzaProofRejection> ProofRejections { get; }
    DbSet<BzaSaleCancellation> SaleCancellations { get; }
    DbSet<BzaBlockedCustomer> BlockedCustomers { get; }
    DbSet<BzaWhatsAppMessage> WhatsAppMessages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}