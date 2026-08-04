using BusinessCloud.Application.Bazares.Commands.ImportGlobalCollectorGroups;
using BusinessCloud.Application.Bazares.Common;
using BusinessCloud.Application.Bazares.Queries.GetGlobalCollectorGroups;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using BusinessCloud.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BusinessCloud.Tests.Application.Bazares;

public class GlobalCollectorCatalogTests
{
    [Theory]
    [InlineData("3.-PLAZA   2000", "PLAZA 2000")]
    [InlineData("10.- HOSPITAL DEL PRADO", "HOSPITAL DEL PRADO")]
    [InlineData("4.1HOSPITAL DEL PRADO", "HOSPITAL DEL PRADO")]
    [InlineData("  ..+ ENVIOS  ", "..+ ENVIOS")]
    public void Clean_RemovesOrderPrefixesAndCollapsesSpaces(string source, string expected)
        => Assert.Equal(expected, CollectorCatalogNameNormalizer.Clean(source));

    [Fact]
    public async Task Import_ReusesNormalizedGroupAndIsIdempotent()
    {
        await using var context = BazaresContextFactory.Create();
        var currentUser = CreateCurrentUser();
        var catalogGroup = new BzaGlobalCollectorGroup
        {
            Id = 5,
            Description = "GRUPO ERA",
            DeliveryFrequency = "QUINCENAL / SEMANAL",
            DeliveryDay = 5,
            Collectors =
            {
                new BzaGlobalCollector { Id = 1, Name = "ANA" },
                new BzaGlobalCollector { Id = 2, Name = "LULU" },
            },
        };
        var tenantGroup = new BzaCollectorGroup
        {
            TenantId = BazaresContextFactory.TenantId,
            Description = "  grupo   era ",
            Collectors =
            {
                new BzaCollector { TenantId = BazaresContextFactory.TenantId, Name = "ana" },
            },
        };
        var otherTenantGroup = new BzaCollectorGroup
        {
            TenantId = "other-tenant",
            Description = "GRUPO ERA",
            Collectors =
            {
                new BzaCollector { TenantId = "other-tenant", Name = "LULU" },
            },
        };
        context.GlobalCollectorGroups.Add(catalogGroup);
        context.CollectorGroups.AddRange(tenantGroup, otherTenantGroup);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new ImportGlobalCollectorGroupsHandler(
            context,
            currentUser.Object,
            new ImportGlobalCollectorGroupsValidator());
        var command = new ImportGlobalCollectorGroupsCommand(false, [catalogGroup.Id]);

        var first = await handler.Handle(command, CancellationToken.None);
        var second = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(0, first.GroupsCreated);
        Assert.Equal(1, first.GroupsReused);
        Assert.Equal(1, first.CollectorsCreated);
        Assert.Equal(1, first.CollectorsSkipped);
        Assert.Equal("QUINCENAL / SEMANAL", tenantGroup.DeliveryFrequency);
        Assert.Equal(5, tenantGroup.DeliveryDay);
        Assert.Equal(2, tenantGroup.Collectors.Count);
        Assert.Equal(0, second.CollectorsCreated);
        Assert.Equal(2, second.CollectorsSkipped);
        Assert.Single(context.CollectorGroups);
        Assert.Equal(2, await context.Collectors.CountAsync());
    }

    [Fact]
    public async Task Query_ReturnsGlobalGroupsWithCollectorsAndMetadata()
    {
        await using var context = BazaresContextFactory.Create();
        context.GlobalCollectorGroups.Add(new BzaGlobalCollectorGroup
        {
            Description = "GRUPO ERA",
            DeliveryFrequency = "SEMANAL",
            DeliveryDay = 5,
            Collectors =
            {
                new BzaGlobalCollector { Name = "ZETA" },
                new BzaGlobalCollector { Name = "ALFA" },
            },
        });
        await context.SaveChangesAsync(CancellationToken.None);
        var currentUser = CreateCurrentUser();
        var handler = new GetGlobalCollectorGroupsHandler(
            context,
            currentUser.Object,
            new GetGlobalCollectorGroupsValidator());

        var result = await handler.Handle(new GetGlobalCollectorGroupsQuery(), CancellationToken.None);

        var group = Assert.Single(result);
        Assert.Equal(2, group.CollectorCount);
        Assert.Equal(5, group.DeliveryDay);
        Assert.Equal("SEMANAL", group.DeliveryFrequency);
        Assert.Equal(["ALFA", "ZETA"], group.Collectors.Select(collector => collector.Name));
    }

    private static Mock<ICurrentUserService> CreateCurrentUser()
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(service => service.TenantId).Returns(BazaresContextFactory.TenantId);
        currentUser.Setup(service => service.GetRequiredTenantId()).Returns(BazaresContextFactory.TenantId);
        return currentUser;
    }
}
