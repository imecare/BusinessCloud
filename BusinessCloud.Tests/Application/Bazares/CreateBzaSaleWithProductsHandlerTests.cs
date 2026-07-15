using BusinessCloud.Application.Bazares.Commands.CreateBzaSaleWithProducts;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using BusinessCloud.Domain.Common.Exceptions;
using BusinessCloud.Infrastructure.Data;
using BusinessCloud.Tests.TestSupport;
using Moq;
using Xunit;

namespace BusinessCloud.Tests.Application.Bazares;

/// <summary>
/// Pruebas del registro de venta con productos: validación de existencia, bloqueo por
/// recolector/grupo inactivo, creación/actualización de la venta, cálculo del total y
/// auditoría en MongoDB.
/// </summary>
public class CreateBzaSaleWithProductsHandlerTests
{
    private const string Tenant = BazaresContextFactory.TenantId;

    private static async Task SeedAsync(
        BazaresDbContext ctx,
        bool withEvent = true,
        bool withCustomer = true,
        bool collectorActive = true,
        bool groupActive = true,
        BzaSale? existingSale = null)
    {
        if (withEvent)
            ctx.Events.Add(new BzaEvent { Id = 1, TenantId = Tenant, Description = "Evento" });

        if (withCustomer)
        {
            var group = new BzaCollectorGroup { Id = 1, TenantId = Tenant, Description = "Grupo A", IsActive = groupActive };
            var collector = new BzaCollector { Id = 1, TenantId = Tenant, Name = "Recolector", IsActive = collectorActive, BzaCollectorGroupId = 1, CollectorGroup = group };
            ctx.Customers.Add(new BzaCustomer
            {
                Id = 1,
                TenantId = Tenant,
                Name = "Ana",
                Phone = "5511112222",
                BzaCollectorId = 1,
                Collector = collector,
            });
        }

        if (existingSale is not null)
            ctx.Sales.Add(existingSale);

        await ctx.SaveChangesAsync(default);
    }

    private static (CreateBzaSaleWithProductsHandler handler, Mock<IMongoContext> mongo) Handler(BazaresDbContext ctx)
    {
        var mongo = new Mock<IMongoContext>();
        return (new CreateBzaSaleWithProductsHandler(ctx, mongo.Object), mongo);
    }

    private static CreateBzaSaleWithProductsCommand Command(params (string desc, decimal price)[] products) =>
        new()
        {
            BzaEventId = 1,
            BzaCustomerId = 1,
            Products = products.Select(p => new CreateBzaSaleProductItem { Description = p.desc, Price = p.price }).ToList(),
        };

    [Fact]
    public async Task Handle_EventoInexistente_LanzaKeyNotFound()
    {
        using var ctx = BazaresContextFactory.Create();
        await SeedAsync(ctx, withEvent: false);
        var (handler, _) = Handler(ctx);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(Command(("Blusa", 100m)), default));
    }

    [Fact]
    public async Task Handle_ClienteInexistente_LanzaKeyNotFound()
    {
        using var ctx = BazaresContextFactory.Create();
        await SeedAsync(ctx, withCustomer: false);
        var (handler, _) = Handler(ctx);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(Command(("Blusa", 100m)), default));
    }

    [Fact]
    public async Task Handle_RecolectorInactivo_LanzaExcepcionConCodigo()
    {
        using var ctx = BazaresContextFactory.Create();
        await SeedAsync(ctx, collectorActive: false);
        var (handler, _) = Handler(ctx);

        var ex = await Assert.ThrowsAsync<SaleCollectorInactiveException>(
            () => handler.Handle(Command(("Blusa", 100m)), default));
        Assert.Equal("COLLECTOR_INACTIVE", ex.Code);
    }

    [Fact]
    public async Task Handle_GrupoInactivo_LanzaExcepcionConCodigo()
    {
        using var ctx = BazaresContextFactory.Create();
        await SeedAsync(ctx, collectorActive: true, groupActive: false);
        var (handler, _) = Handler(ctx);

        var ex = await Assert.ThrowsAsync<SaleCollectorInactiveException>(
            () => handler.Handle(Command(("Blusa", 100m)), default));
        Assert.Equal("COLLECTOR_GROUP_INACTIVE", ex.Code);
    }

    [Fact]
    public async Task Handle_CreaVentaCalculaTotalYAuditaEnMongo()
    {
        using var ctx = BazaresContextFactory.Create();
        await SeedAsync(ctx);
        var (handler, mongo) = Handler(ctx);

        var result = await handler.Handle(Command(("Blusa", 100m), ("Falda", 150m)), default);

        Assert.Equal(2, result.ProductsAdded);
        Assert.Equal(250m, result.Total);
        mongo.Verify(m => m.InsertAuditLogAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_VentaCerrada_LanzaExcepcion()
    {
        using var ctx = BazaresContextFactory.Create();
        var closed = new BzaSale { Id = 5, TenantId = Tenant, BzaEventId = 1, BzaCustomerId = 1, IsClosed = true };
        await SeedAsync(ctx, existingSale: closed);
        var (handler, _) = Handler(ctx);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(Command(("Blusa", 100m)), default));
    }

    [Fact]
    public async Task Handle_VentaExistenteAbierta_SumaTotalConProductosPrevios()
    {
        using var ctx = BazaresContextFactory.Create();
        var open = new BzaSale
        {
            Id = 5,
            TenantId = Tenant,
            BzaEventId = 1,
            BzaCustomerId = 1,
            IsClosed = false,
            Products = new List<BzaSoldProduct> { new() { Id = 1, TenantId = Tenant, Description = "Previo", Price = 50m } },
        };
        await SeedAsync(ctx, existingSale: open);
        var (handler, _) = Handler(ctx);

        var result = await handler.Handle(Command(("Nuevo", 100m)), default);

        Assert.Equal(1, result.ProductsAdded);   // solo el nuevo se cuenta como agregado
        Assert.Equal(150m, result.Total);         // pero el total incluye el previo
    }
}
