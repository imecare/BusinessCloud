using BusinessCloud.Application.Bazares.Commands.CreateCollectorGroup;
using BusinessCloud.Application.Bazares.Commands.UpdateCollectorGroup;
using BusinessCloud.Domain.Bazares.Entities;
using BusinessCloud.Tests.TestSupport;
using Xunit;

namespace BusinessCloud.Tests.Application.Bazares;

public class CollectorGroupSchedulingTests
{
    [Fact]
    public async Task Create_WithoutDeliveryDay_SavesFriday()
    {
        using var context = BazaresContextFactory.Create();
        var handler = new CreateCollectorGroupHandler(context);

        var id = await handler.Handle(new CreateCollectorGroupCommand("Grupo Centro"), default);

        var group = context.CollectorGroups.Single(item => item.Id == id);
        Assert.Equal((int)DayOfWeek.Friday, group.DeliveryDay);
    }

    [Fact]
    public async Task Update_WithoutDeliveryDay_SavesFriday()
    {
        using var context = BazaresContextFactory.Create();
        context.CollectorGroups.Add(new BzaCollectorGroup
        {
            Id = 7,
            TenantId = BazaresContextFactory.TenantId,
            Description = "Grupo Centro",
            DeliveryDay = null,
        });
        await context.SaveChangesAsync(default);
        var handler = new UpdateCollectorGroupHandler(context);

        await handler.Handle(new UpdateCollectorGroupCommand(7, "Grupo Centro"), default);

        Assert.Equal((int)DayOfWeek.Friday, context.CollectorGroups.Single().DeliveryDay);
    }
}