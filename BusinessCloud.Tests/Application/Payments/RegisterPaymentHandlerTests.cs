using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Payments.Commands.RegisterPayment;
using BusinessCloud.Domain.Payments.Entities;
using BusinessCloud.Infrastructure.Data;
using BusinessCloud.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BusinessCloud.Tests.Application.Payments;

/// <summary>
/// Pruebas del registro de abonos: idempotencia por caché, recálculo de "pagado" (IsPaid),
/// cálculo del nuevo saldo, folio y almacenamiento en caché del resultado.
/// </summary>
public class RegisterPaymentHandlerTests
{
    private const string Tenant = PaymentsContextFactory.TenantId;

    private static async Task SeedSaleAsync(PaymentsDbContext ctx, decimal total, decimal previousPaid = 0m)
    {
        ctx.Customers.Add(new Customer { Id = 1, TenantId = Tenant, Name = "Ana", Phone = "5511112222" });
        ctx.Sales.Add(new Sale { Id = 1, TenantId = Tenant, CustomerId = 1, TotalAmount = total, ProductDescription = "P" });
        if (previousPaid > 0m)
            ctx.Payments.Add(new Payment { Id = 1, TenantId = Tenant, SaleId = 1, Amount = previousPaid, PaymentTypeId = 2 });
        await ctx.SaveChangesAsync(default);
    }

    private static (RegisterPaymentHandler handler, Mock<ICacheService> cache) Handler(PaymentsDbContext ctx)
    {
        var mongo = new Mock<IMongoContext>();
        mongo.Setup(m => m.InsertAuditLogAsync(It.IsAny<object>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mongo.Setup(m => m.UpdateCustomerReadModelAsync(It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var cache = new Mock<ICacheService>();
        cache.Setup(c => c.RemoveAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        var handler = new RegisterPaymentHandler(ctx, mongo.Object, cache.Object, NullLogger<RegisterPaymentHandler>.Instance);
        return (handler, cache);
    }

    [Fact]
    public async Task Handle_ConResultadoEnCache_DevuelveElCacheadoSinRegistrar()
    {
        using var ctx = PaymentsContextFactory.Create(); // sin venta sembrada
        var (handler, cache) = Handler(ctx);
        var cached = new PaymentReceiptDto("PAY-99", "Ana", 100m, 0m, DateTime.UtcNow, DateTime.UtcNow, new());
        cache.Setup(c => c.GetAsync<PaymentReceiptDto>(It.IsAny<string>())).ReturnsAsync(cached);

        var result = await handler.Handle(
            new RegisterPaymentCommand(1, 100m, "Efectivo", DateTime.UtcNow, IdempotencyKey: "key-1"), default);

        Assert.Equal("PAY-99", result.Folio);           // devolvió el cacheado
        Assert.Empty(await GetPaymentsAsync(ctx));       // no registró ningún abono
    }

    [Fact]
    public async Task Handle_VentaInexistente_LanzaExcepcion()
    {
        using var ctx = PaymentsContextFactory.Create();
        var (handler, _) = Handler(ctx);

        await Assert.ThrowsAsync<Exception>(() => handler.Handle(
            new RegisterPaymentCommand(1, 100m, "Efectivo", DateTime.UtcNow), default));
    }

    [Fact]
    public async Task Handle_AbonoCubreElTotal_MarcaVentaComoPagada()
    {
        using var ctx = PaymentsContextFactory.Create();
        await SeedSaleAsync(ctx, total: 300m, previousPaid: 200m);
        var (handler, _) = Handler(ctx);

        await handler.Handle(new RegisterPaymentCommand(1, 100m, "Efectivo", DateTime.UtcNow), default);

        var sale = await ctx.Sales.FindAsync(1);
        Assert.True(sale!.IsPaid);
    }

    [Fact]
    public async Task Handle_AbonoNoCubreElTotal_NoMarcaComoPagada()
    {
        using var ctx = PaymentsContextFactory.Create();
        await SeedSaleAsync(ctx, total: 300m, previousPaid: 50m);
        var (handler, _) = Handler(ctx);

        var result = await handler.Handle(new RegisterPaymentCommand(1, 100m, "Efectivo", DateTime.UtcNow), default);

        var sale = await ctx.Sales.FindAsync(1);
        Assert.False(sale!.IsPaid);
        Assert.Equal(150m, result.NewBalance); // 300 - 50 - 100
    }

    [Fact]
    public async Task Handle_GeneraFolioConIdDelPago()
    {
        using var ctx = PaymentsContextFactory.Create();
        await SeedSaleAsync(ctx, total: 500m);
        var (handler, _) = Handler(ctx);

        var result = await handler.Handle(new RegisterPaymentCommand(1, 100m, "Efectivo", DateTime.UtcNow), default);

        Assert.StartsWith("PAY-", result.Folio);
    }

    [Fact]
    public async Task Handle_ConIdempotencyKey_GuardaResultadoEnCache()
    {
        using var ctx = PaymentsContextFactory.Create();
        await SeedSaleAsync(ctx, total: 500m);
        var (handler, cache) = Handler(ctx);
        cache.Setup(c => c.GetAsync<PaymentReceiptDto>(It.IsAny<string>())).ReturnsAsync((PaymentReceiptDto?)null);

        await handler.Handle(new RegisterPaymentCommand(1, 100m, "Efectivo", DateTime.UtcNow, IdempotencyKey: "key-1"), default);

        cache.Verify(c => c.SetAsync(
            It.Is<string>(k => k.Contains("key-1")),
            It.IsAny<PaymentReceiptDto>(),
            It.IsAny<TimeSpan?>()), Times.Once);
    }

    private static async Task<List<Payment>> GetPaymentsAsync(PaymentsDbContext ctx)
        => await Task.FromResult(ctx.Payments.ToList());
}
