using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Common;

public static class NoCollectorCustomer
{
    public const string DisplayName = "Aún sin recolector";

    public static bool IsNoCollectorName(string? value)
        => CollectorCatalogNameNormalizer.ToComparisonKey(value) == "AUN SIN RECOLECTOR";

    public static async Task<BzaCollector> GetOrCreateAsync(IBazaresDbContext context, CancellationToken ct)
    {
        var collector = await context.Collectors
            .FirstOrDefaultAsync(c => c.Name == DisplayName, ct);

        if (collector is not null)
        {
            if (!collector.IsActive)
            {
                collector.IsActive = true;
            }

            return collector;
        }

        collector = new BzaCollector
        {
            Name = DisplayName,
            BzaCollectorGroupId = null,
            IsActive = true,
        };

        context.Collectors.Add(collector);
        return collector;
    }
}
