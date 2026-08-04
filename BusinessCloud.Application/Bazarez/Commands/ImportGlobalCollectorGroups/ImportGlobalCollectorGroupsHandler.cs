using BusinessCloud.Application.Bazares.Common;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.ImportGlobalCollectorGroups;

public class ImportGlobalCollectorGroupsHandler(
    IBazaresDbContext context,
    ICurrentUserService currentUser,
    IValidator<ImportGlobalCollectorGroupsCommand> validator)
    : IRequestHandler<ImportGlobalCollectorGroupsCommand, ImportGlobalCollectorGroupsResult>
{
    public async Task<ImportGlobalCollectorGroupsResult> Handle(
        ImportGlobalCollectorGroupsCommand request,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        var tenantId = currentUser.GetRequiredTenantId();
        ImportGlobalCollectorGroupsResult? result = null;

        await context.ExecuteInTransactionAsync(async transactionCt =>
        {
            var selectedIds = request.GroupIds?.ToHashSet() ?? [];
            var catalogQuery = context.GlobalCollectorGroups
                .AsNoTracking()
                .Include(group => group.Collectors)
                .AsQueryable();

            if (!request.ImportAll)
            {
                catalogQuery = catalogQuery.Where(group => selectedIds.Contains(group.Id));
            }

            var catalogGroups = await catalogQuery
                .OrderBy(group => group.Id)
                .ToListAsync(transactionCt);

            if (!request.ImportAll && catalogGroups.Count != selectedIds.Count)
            {
                var foundIds = catalogGroups.Select(group => group.Id).ToHashSet();
                var missingIds = selectedIds.Except(foundIds).OrderBy(id => id);
                throw new KeyNotFoundException(
                    $"No se encontraron los grupos globales: {string.Join(", ", missingIds)}.");
            }

            var tenantGroups = await context.CollectorGroups
                .Include(group => group.Collectors)
                .OrderBy(group => group.Id)
                .ToListAsync(transactionCt);

            var tenantGroupByName = tenantGroups
                .GroupBy(group => CollectorCatalogNameNormalizer.ToComparisonKey(group.Description))
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

            var groupsCreated = 0;
            var groupsReused = 0;
            var collectorsCreated = 0;
            var collectorsSkipped = 0;
            var groupResults = new List<ImportedCollectorGroupResult>(catalogGroups.Count);

            foreach (var catalogGroup in catalogGroups)
            {
                var groupKey = CollectorCatalogNameNormalizer.ToComparisonKey(catalogGroup.Description);
                var groupCreated = !tenantGroupByName.TryGetValue(groupKey, out var tenantGroup);

                if (groupCreated)
                {
                    tenantGroup = new BzaCollectorGroup
                    {
                        TenantId = tenantId,
                        Description = catalogGroup.Description,
                        DeliveryFrequency = catalogGroup.DeliveryFrequency,
                        DeliveryDay = catalogGroup.DeliveryDay,
                        IsActive = true,
                    };
                    context.CollectorGroups.Add(tenantGroup);
                    tenantGroupByName[groupKey] = tenantGroup;
                    groupsCreated++;
                }
                else
                {
                    groupsReused++;
                    if (string.IsNullOrWhiteSpace(tenantGroup!.DeliveryFrequency))
                    {
                        tenantGroup.DeliveryFrequency = catalogGroup.DeliveryFrequency;
                    }

                    if (!tenantGroup.DeliveryDay.HasValue)
                    {
                        tenantGroup.DeliveryDay = catalogGroup.DeliveryDay;
                    }
                }

                var existingCollectors = tenantGroup!.Collectors
                    .Select(collector => CollectorCatalogNameNormalizer.ToComparisonKey(collector.Name))
                    .ToHashSet(StringComparer.Ordinal);
                var createdForGroup = 0;
                var skippedForGroup = 0;

                foreach (var catalogCollector in catalogGroup.Collectors.OrderBy(collector => collector.Id))
                {
                    var collectorKey = CollectorCatalogNameNormalizer.ToComparisonKey(catalogCollector.Name);
                    if (!existingCollectors.Add(collectorKey))
                    {
                        collectorsSkipped++;
                        skippedForGroup++;
                        continue;
                    }

                    tenantGroup.Collectors.Add(new BzaCollector
                    {
                        TenantId = tenantId,
                        Name = catalogCollector.Name,
                        IsActive = true,
                    });
                    collectorsCreated++;
                    createdForGroup++;
                }

                groupResults.Add(new ImportedCollectorGroupResult(
                    catalogGroup.Id,
                    catalogGroup.Description,
                    groupCreated,
                    createdForGroup,
                    skippedForGroup));
            }

            await context.SaveChangesAsync(transactionCt);
            result = new ImportGlobalCollectorGroupsResult(
                catalogGroups.Count,
                groupsCreated,
                groupsReused,
                collectorsCreated,
                collectorsSkipped,
                groupResults);
        }, ct);

        return result!;
    }
}
