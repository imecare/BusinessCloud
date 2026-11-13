using BusinessCloud.Application.Catalogs.Commands.Orders;
using BusinessCloud.Application.Catalogs.Commands.Public;
using BusinessCloud.Application.Catalogs.Dtos;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Catalogs.Entities;
using BusinessCloud.Domain.Common.Entities;
using BusinessCloud.Infrastructure.Data;
using BusinessCloud.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BusinessCloud.Tests.Application.Catalogs;

/// <summary>Pedidos públicos: descuento de existencia, falta de stock, restitución y notificación.</summary>
public class PublicOrderTests
{
    private const string Tenant = CatalogsContextFactory.TenantId;
    private const string Slug = "mi-tienda";

    private static async Task<IdentityDbContext> SeedIdentityAsync(
        bool moduleActive = true, bool suspended = false)
    {
        var identity = IdentityContextFactory.Create();
        identity.Tenants.Add(new Tenant { Id = Tenant, Name = "Mi Tienda", IsActive = true });
        identity.TenantModules.Add(new TenantModule
        {
            TenantId = Tenant, Module = SystemModules.Catalogs, IsActive = moduleActive,
        });
        if (suspended)
        {
            identity.TenantSubscriptions.Add(new TenantSubscription
            {
                TenantId = Tenant, PaidUntil = DateTime.UtcNow.AddDays(-60), GraceDays = 5,
            });
        }
        await identity.SaveChangesAsync();
        return identity;
    }

    private static async Task<(int productId, int variantId)> SeedProductAsync(
        CatalogsDbContext db, int stock, CatProductStatus status = CatProductStatus.Active, decimal price = 100m)
    {
        if (!await db.StoreSettings.AnyAsync())
            db.StoreSettings.Add(new CatStoreSettings { TenantId = Tenant, Slug = Slug, PublicName = "Mi Tienda" });

        var product = new CatProduct { TenantId = Tenant, Name = "Blusa", Price = price, Status = status };
        product.Variants.Add(new CatProductVariant { TenantId = Tenant, Size = "M", Stock = stock });
        db.Products.Add(product);
        await db.SaveChangesAsync(default);
        return (product.Id, product.Variants.First().Id);
    }

    private static (CreatePublicOrderHandler handler, Mock<IEmailSender> email) Handler(
        CatalogsDbContext db, IdentityDbContext identity, bool emailThrows = false)
    {
        var email = new Mock<IEmailSender>();
        email.SetupGet(e => e.IsConfigured).Returns(true);
        if (emailThrows)
            email.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("smtp caído"));
        else
            email.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new EmailSendResult(true, "id", null, null));

        return (new CreatePublicOrderHandler(db, identity, email.Object, NullLogger<CreatePublicOrderHandler>.Instance), email);
    }

    private static CreatePublicOrderCommand Order(int productId, int variantId, int qty) => new(Slug, new PublicOrderInput
    {
        CustomerName = "Ana",
        CustomerPhone = "5511223344",
        Items = [new PublicOrderItemInput { ProductId = productId, VariantId = variantId, Quantity = qty }],
    });

    [Fact]
    public async Task Pedido_con_existencia_descuenta_stock_y_notifica()
    {
        using var db = CatalogsContextFactory.Create();
        using var identity = await SeedIdentityAsync();
        var (productId, variantId) = await SeedProductAsync(db, stock: 5);
        var (handler, email) = Handler(db, identity);
        identity.Users.Add(new ApplicationUser { Id = "u1", TenantId = Tenant, Email = "dueña@tienda.com", FirstName = "A", LastName = "B", Role = SystemRoles.SuperAdmin });
        await identity.SaveChangesAsync();

        var result = await handler.Handle(Order(productId, variantId, 2), default);

        Assert.False(result.HasOutOfStockItems);
        Assert.Equal(200m, result.EstimatedTotal);
        Assert.Equal("P-000001", result.OrderNumber);
        var variant = await db.ProductVariants.SingleAsync();
        Assert.Equal(3, variant.Stock);
        email.Verify(e => e.SendAsync("dueña@tienda.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Pedido_sin_existencia_suficiente_no_se_rechaza_y_marca_la_linea()
    {
        using var db = CatalogsContextFactory.Create();
        using var identity = await SeedIdentityAsync();
        var (productId, variantId) = await SeedProductAsync(db, stock: 1);
        var (handler, _) = Handler(db, identity);

        var result = await handler.Handle(Order(productId, variantId, 3), default);

        Assert.True(result.HasOutOfStockItems);
        var item = await db.OrderItems.SingleAsync();
        Assert.True(item.OutOfStockAtSubmit);
        Assert.Equal(1, item.QuantityDiscounted);
        Assert.Equal(3, item.RequestedQuantity);
        Assert.Contains("Sin existencia", item.LineNote);
        Assert.Equal(0, (await db.ProductVariants.SingleAsync()).Stock);
    }

    [Fact]
    public async Task Producto_marcado_agotado_se_acepta_sin_descontar()
    {
        using var db = CatalogsContextFactory.Create();
        using var identity = await SeedIdentityAsync();
        var (productId, variantId) = await SeedProductAsync(db, stock: 4, status: CatProductStatus.SoldOut);
        var (handler, _) = Handler(db, identity);

        var result = await handler.Handle(Order(productId, variantId, 1), default);

        Assert.True(result.HasOutOfStockItems);
        Assert.Equal(4, (await db.ProductVariants.SingleAsync()).Stock);
    }

    [Fact]
    public async Task Producto_oculto_rechaza_la_linea()
    {
        using var db = CatalogsContextFactory.Create();
        using var identity = await SeedIdentityAsync();
        var (productId, variantId) = await SeedProductAsync(db, stock: 4, status: CatProductStatus.Hidden);
        var (handler, _) = Handler(db, identity);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(Order(productId, variantId, 1), default));
        Assert.Empty(await db.Orders.ToListAsync());
    }

    [Fact]
    public async Task Fallo_de_correo_no_rompe_el_pedido()
    {
        using var db = CatalogsContextFactory.Create();
        using var identity = await SeedIdentityAsync();
        var (productId, variantId) = await SeedProductAsync(db, stock: 2);
        db.StoreSettings.Single().OrderEmail = "pedidos@tienda.com";
        await db.SaveChangesAsync(default);
        var (handler, _) = Handler(db, identity, emailThrows: true);

        var result = await handler.Handle(Order(productId, variantId, 1), default);

        Assert.Equal("P-000001", result.OrderNumber);
        Assert.Single(await db.Orders.ToListAsync());
    }

    [Theory]
    [InlineData("", "5511223344")]
    [InlineData("Ana", "123")]
    [InlineData("Ana", "abc")]
    public async Task Datos_de_cliente_invalidos_se_rechazan(string name, string phone)
    {
        using var db = CatalogsContextFactory.Create();
        using var identity = await SeedIdentityAsync();
        var (productId, variantId) = await SeedProductAsync(db, stock: 2);
        var (handler, _) = Handler(db, identity);
        var command = Order(productId, variantId, 1);
        command.Input.CustomerName = name;
        command.Input.CustomerPhone = phone;

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, default));
    }

    [Fact]
    public async Task Cancelar_pedido_devuelve_la_existencia_y_no_se_puede_reabrir()
    {
        using var db = CatalogsContextFactory.Create();
        using var identity = await SeedIdentityAsync();
        var (productId, variantId) = await SeedProductAsync(db, stock: 5);
        var (handler, _) = Handler(db, identity);
        await handler.Handle(Order(productId, variantId, 2), default);
        var orderId = (await db.Orders.SingleAsync()).Id;
        var statusHandler = new SetOrderStatusHandler(db, CatalogsContextFactory.CurrentUser());

        await statusHandler.Handle(new SetOrderStatusCommand(orderId, "Cancelled"), default);

        Assert.Equal(5, (await db.ProductVariants.SingleAsync()).Stock);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => statusHandler.Handle(new SetOrderStatusCommand(orderId, "Confirmed"), default));
        // Cancelar dos veces no debe devolver existencia doble.
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => statusHandler.Handle(new SetOrderStatusCommand(orderId, "Cancelled"), default));
        Assert.Equal(5, (await db.ProductVariants.SingleAsync()).Stock);
    }

    [Fact]
    public async Task Confirmar_con_cantidad_menor_devuelve_la_diferencia_y_recalcula_total()
    {
        using var db = CatalogsContextFactory.Create();
        using var identity = await SeedIdentityAsync();
        var (productId, variantId) = await SeedProductAsync(db, stock: 5);
        var (handler, _) = Handler(db, identity);
        await handler.Handle(Order(productId, variantId, 3), default);
        var order = await db.Orders.Include(o => o.Items).SingleAsync();
        var user = CatalogsContextFactory.CurrentUser();

        var adjusted = await new AdjustOrderHandler(db, user).Handle(
            new AdjustOrderCommand(order.Id, new OrderAdjustInput
            {
                InternalNote = "Solo hay 1",
                Items = [new OrderItemAdjustInput { Id = order.Items.First().Id, FinalQuantity = 1 }],
            }), default);
        Assert.Equal(100m, adjusted.FinalTotal);

        await new SetOrderStatusHandler(db, user).Handle(new SetOrderStatusCommand(order.Id, "Confirmed"), default);

        // 5 - 3 (descontado al pedir) + 2 (devueltas al confirmar solo 1) = 4
        Assert.Equal(4, (await db.ProductVariants.SingleAsync()).Stock);
        Assert.Equal(CatOrderStatus.Confirmed, (await db.Orders.SingleAsync()).Status);
    }

    [Fact]
    public async Task Ajustar_cantidad_mayor_a_la_solicitada_se_rechaza()
    {
        using var db = CatalogsContextFactory.Create();
        using var identity = await SeedIdentityAsync();
        var (productId, variantId) = await SeedProductAsync(db, stock: 5);
        var (handler, _) = Handler(db, identity);
        await handler.Handle(Order(productId, variantId, 1), default);
        var order = await db.Orders.Include(o => o.Items).SingleAsync();

        await Assert.ThrowsAsync<ArgumentException>(() => new AdjustOrderHandler(db, CatalogsContextFactory.CurrentUser()).Handle(
            new AdjustOrderCommand(order.Id, new OrderAdjustInput
            {
                Items = [new OrderItemAdjustInput { Id = order.Items.First().Id, FinalQuantity = 2 }],
            }), default));
    }

    [Fact]
    public async Task Catalogo_con_suscripcion_suspendida_no_esta_disponible()
    {
        using var db = CatalogsContextFactory.Create();
        using var identity = await SeedIdentityAsync(suspended: true);
        var (productId, variantId) = await SeedProductAsync(db, stock: 5);
        var (handler, _) = Handler(db, identity);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(Order(productId, variantId, 1), default));
        Assert.Equal("Catálogo no disponible", ex.Message);
    }

    [Fact]
    public async Task Catalogo_sin_modulo_activo_no_esta_disponible()
    {
        using var db = CatalogsContextFactory.Create();
        using var identity = await SeedIdentityAsync(moduleActive: false);
        var (productId, variantId) = await SeedProductAsync(db, stock: 5);
        var (handler, _) = Handler(db, identity);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(Order(productId, variantId, 1), default));
    }
}
