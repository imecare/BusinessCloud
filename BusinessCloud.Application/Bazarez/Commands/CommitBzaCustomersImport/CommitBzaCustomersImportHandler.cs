using BusinessCloud.Application.Bazares.Common;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.CommitBzaCustomersImport;

public class CommitBzaCustomersImportHandler(
    IBazaresDbContext context,
    IMongoContext mongoContext,
    ICurrentUserService currentUser)
    : IRequestHandler<CommitBzaCustomersImportCommand, CommitBzaCustomersImportResult>
{
    public async Task<CommitBzaCustomersImportResult> Handle(
        CommitBzaCustomersImportCommand request,
        CancellationToken ct)
    {
        var result = new CommitBzaCustomersImportResult();
        var tenantId = currentUser.GetRequiredTenantId();

        await context.ExecuteInTransactionAsync(async transactionCt =>
        {
            var collectors = await context.Collectors.ToListAsync(transactionCt);
            var collectorByKey = collectors
                .GroupBy(collector => CollectorCatalogNameNormalizer.ToComparisonKey(collector.Name), StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);

            foreach (var requestedCollector in request.NewCollectors)
            {
                var name = CollectorCatalogNameNormalizer.Clean(requestedCollector.Name);
                var key = CollectorCatalogNameNormalizer.ToComparisonKey(name);
                if (key.Length == 0 || key == "SIN ASIGNAR")
                {
                    result.Errors.Add($"Recolector nuevo '{requestedCollector.Name}' IGNORADO: el nombre no es válido.");
                    continue;
                }

                if (collectorByKey.ContainsKey(key))
                    continue;

                var groupExists = await context.CollectorGroups
                    .AnyAsync(group => group.Id == requestedCollector.GroupId, transactionCt);
                if (!groupExists)
                {
                    result.Errors.Add($"Grupo inválido para el recolector nuevo '{name}'.");
                    continue;
                }

                var collector = new BzaCollector
                {
                    Name = name,
                    BzaCollectorGroupId = requestedCollector.GroupId,
                    IsActive = true,
                };
                context.Collectors.Add(collector);
                collectorByKey[key] = [collector];
                result.NewCollectorsCreated++;
            }

            var existingCustomers = await context.Customers
                .Select(customer => new { customer.Name, customer.Phone })
                .ToListAsync(transactionCt);
            var existingNames = existingCustomers
                .Select(customer => NormalizeCustomerKey(customer.Name))
                .ToHashSet(StringComparer.Ordinal);
            var phoneOwners = existingCustomers
                .Where(customer => !string.IsNullOrWhiteSpace(customer.Phone))
                .GroupBy(customer => customer.Phone.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First().Name, StringComparer.OrdinalIgnoreCase);

            foreach (var dto in request.Customers)
            {
                var name = CollectorCatalogNameNormalizer.CollapseSpaces(dto.Name);
                var nameKey = NormalizeCustomerKey(name);
                if (name.Length == 0)
                {
                    result.Errors.Add("Cliente sin nombre. Se omitió.");
                    result.IgnoredRecords++;
                    continue;
                }

                if (!existingNames.Add(nameKey))
                {
                    result.Errors.Add($"Cliente '{name}' IGNORADO: ya está registrado.");
                    result.IgnoredRecords++;
                    continue;
                }

                var collectorKey = CollectorCatalogNameNormalizer.ToComparisonKey(dto.CollectorName);
                if (collectorKey.Length == 0 || collectorKey == "SIN ASIGNAR")
                {
                    result.Errors.Add($"Cliente '{name}' IGNORADO: debe tener un recolector real.");
                    result.IgnoredRecords++;
                    existingNames.Remove(nameKey);
                    continue;
                }

                if (!collectorByKey.TryGetValue(collectorKey, out var collectorMatches))
                {
                    result.Errors.Add($"Cliente '{name}' IGNORADO: recolector '{dto.CollectorName}' no encontrado.");
                    result.IgnoredRecords++;
                    existingNames.Remove(nameKey);
                    continue;
                }

                if (collectorMatches.Count != 1)
                {
                    result.Errors.Add(
                        $"Cliente '{name}' IGNORADO: el recolector '{dto.CollectorName}' es ambiguo entre varios grupos.");
                    result.IgnoredRecords++;
                    existingNames.Remove(nameKey);
                    continue;
                }

                var phone = PhoneNumberNormalizer.Normalize(dto.Phone);
                if (phone.Length > 0 && phoneOwners.TryGetValue(phone, out var owner))
                {
                    result.Errors.Add(
                        $"Cliente '{name}' IGNORADO: el teléfono '{phone}' ya está registrado para '{owner}'.");
                    result.IgnoredRecords++;
                    existingNames.Remove(nameKey);
                    continue;
                }

                var facebookName = FacebookMessengerProfile.Normalize(dto.FacebookName);
                var hasNoWhatsApp = phone.Length == 0;
                if (hasNoWhatsApp)
                    phone = await NoWhatsAppNumber.ReserveNextAsync(context, tenantId, transactionCt);

                var isPendingInfo = hasNoWhatsApp && facebookName is null;
                var collector = collectorMatches[0];
                context.Customers.Add(new BzaCustomer
                {
                    Name = name,
                    Phone = phone,
                    HasNoWhatsApp = hasNoWhatsApp,
                    FacebookName = facebookName,
                    BzaCollectorId = collector.Id,
                    Collector = collector,
                    Status = 1,
                    IsPendingInfo = isPendingInfo,
                    PortalToken = Guid.NewGuid().ToString("N")[..12],
                });

                phoneOwners[phone] = name;
                result.CustomersCreated++;
                if (isPendingInfo)
                    result.PendingInfoCustomersCreated++;
            }

            await context.SaveChangesAsync(transactionCt);
        }, ct);

        await mongoContext.InsertAuditLogAsync(new
        {
            Event = "Bza_CustomersImportedFromExcel",
            result.CustomersCreated,
            result.PendingInfoCustomersCreated,
            result.NewCollectorsCreated,
            result.IgnoredRecords,
            Source = "Excel",
            Timestamp = DateTime.UtcNow,
        }, ct);

        return result;
    }

    private static string NormalizeCustomerKey(string? value)
        => CollectorCatalogNameNormalizer.CollapseSpaces(value).ToUpperInvariant();
}
