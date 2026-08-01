using BusinessCloud.Application.Bazares.Commands.DeleteLiveSaleDrafts;
using BusinessCloud.Application.Bazares.Commands.SaveLiveSaleRow;
using BusinessCloud.Application.Bazares.Commands.UpdateBzaSoldProduct;
using BusinessCloud.Application.Bazares.Queries.GetLiveSaleDrafts;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using BusinessCloud.Infrastructure.Data;
using BusinessCloud.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BusinessCloud.Tests.Application.Bazares;

public class LiveSaleDraftHandlerTests
{
    private const string Tenant = BazaresContextFactory.TenantId;

    private static async Task SeedAsync(BazaresDbContext context)
    {
        var group = new BzaCollectorGroup { Id = 1, TenantId = Tenant, Description = "Grupo", IsActive = true };
        var collector = new BzaCollector
        {
            Id = 1,
            TenantId = Tenant,
            Name = "Recolector",
            IsActive = true,
            BzaCollectorGroupId = group.Id,
            CollectorGroup = group,
        };
        context.Events.Add(new BzaEvent { Id = 1, TenantId = Tenant, Description = "En vivo", Status = 1 });
        context.Customers.Add(new BzaCustomer
        {
            Id = 1,
            TenantId = Tenant,
            Name = "Cliente",
            Phone = "5512345678",
            BzaCollectorId = collector.Id,
            Collector = collector,
        });
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task SaveLiveRow_SinCliente_PersisteBorrador()
    {
        using var context = BazaresContextFactory.Create();
        await SeedAsync(context);
        var handler = new SaveLiveSaleRowHandler(context, Mock.Of<IMongoContext>());

        var result = await handler.Handle(new SaveLiveSaleRowCommand
        {
            BzaEventId = 1,
            Description = "Blusa",
            Price = 250m,
        }, default);

        Assert.False(result.Assigned);
        Assert.NotNull(result.DraftId);
        Assert.Equal("Blusa", (await context.LiveSaleDrafts.SingleAsync()).Description);
    }

    [Fact]
    public async Task SaveLiveRow_ConCliente_ConvierteBorradorEnProducto()
    {
        using var context = BazaresContextFactory.Create();
        await SeedAsync(context);
        context.LiveSaleDrafts.Add(new BzaLiveSaleDraft
        {
            Id = 10,
            TenantId = Tenant,
            BzaEventId = 1,
            Description = "Blusa",
            Price = 250m,
        });
        await context.SaveChangesAsync();
        var handler = new SaveLiveSaleRowHandler(context, Mock.Of<IMongoContext>());

        var result = await handler.Handle(new SaveLiveSaleRowCommand
        {
            DraftId = 10,
            BzaEventId = 1,
            BzaCustomerId = 1,
            Description = "Blusa",
            Price = 250m,
        }, default);

        Assert.True(result.Assigned);
        Assert.Empty(context.LiveSaleDrafts);
        Assert.Equal(250m, (await context.SoldProducts.SingleAsync()).Price);
    }

    [Fact]
    public async Task GetAndDeleteLiveDrafts_OperaSoloSobreEventoIndicado()
    {
        using var context = BazaresContextFactory.Create();
        await SeedAsync(context);
        context.LiveSaleDrafts.AddRange(
            new BzaLiveSaleDraft { TenantId = Tenant, BzaEventId = 1, Description = "Uno", Price = 10m },
            new BzaLiveSaleDraft { TenantId = Tenant, BzaEventId = 1, Description = "Dos", Price = 20m });
        await context.SaveChangesAsync();

        var listed = await new GetLiveSaleDraftsHandler(context).Handle(new GetLiveSaleDraftsQuery(1), default);
        var deleted = await new DeleteLiveSaleDraftsHandler(context).Handle(new DeleteLiveSaleDraftsCommand(1), default);

        Assert.Equal(2, listed.Count);
        Assert.Equal(2, deleted);
        Assert.Empty(context.LiveSaleDrafts);
    }
    [Fact]
    public async Task UpdateSoldProduct_ReasignaSoloElProductoYActualizaDatos()
    {
        using var context = BazaresContextFactory.Create();
        await SeedAsync(context);
        var destinationCustomer = new BzaCustomer
        {
            TenantId = Tenant,
            Name = "Cliente destino",
            Phone = "5511111111",
            BzaCollectorId = 1,
            Status = 1,
        };
        context.Customers.Add(destinationCustomer);
        var sourceSale = new BzaSale
        {
            Id = 20,
            TenantId = Tenant,
            BzaEventId = 1,
            BzaCustomerId = 1,
            Products =
            [
                new BzaSoldProduct { Id = 30, TenantId = Tenant, Description = "Blusa", Price = 250m },
                new BzaSoldProduct { Id = 31, TenantId = Tenant, Description = "Bolsa", Price = 300m },
            ],
        };
        context.Sales.Add(sourceSale);
        await context.SaveChangesAsync();
        var handler = new UpdateBzaSoldProductHandler(context, Mock.Of<IMongoContext>());

        var updated = await handler.Handle(new UpdateBzaSoldProductCommand
        {
            Id = 30,
            BzaCustomerId = destinationCustomer.Id,
            Description = "Blusa corregida",
            Price = 275m,
        }, default);

        var product = await context.SoldProducts.Include(x => x.Sale).SingleAsync(x => x.Id == 30);
        Assert.True(updated);
        Assert.Equal("Blusa corregida", product.Description);
        Assert.Equal(275m, product.Price);
        Assert.Equal(destinationCustomer.Id, product.Sale.BzaCustomerId);
        Assert.Single(await context.SoldProducts.Where(x => x.BzaSaleId == sourceSale.Id).ToListAsync());
    }
}
