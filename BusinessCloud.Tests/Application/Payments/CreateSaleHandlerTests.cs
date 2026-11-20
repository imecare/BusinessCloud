using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Payments.Commands.CreateSale;
using BusinessCloud.Application.Payments.Commands.DeletePayment;
using BusinessCloud.Application.Payments.Commands.RegisterPayment;
using BusinessCloud.Application.Payments.Commands.UpdatePayment;
using BusinessCloud.Application.Payments.Commands.UpdateSale;
using BusinessCloud.Application.Payments.Queries.GetAllSales;
using BusinessCloud.Application.Payments.Queries.GetPublicHistory;
using BusinessCloud.Domain.Common.Entities;
using BusinessCloud.Domain.Payments.Entities;
using BusinessCloud.Infrastructure.Data;
using BusinessCloud.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BusinessCloud.Tests.Application.Payments;

public class CreateSaleHandlerTests
{
    internal static CreateSaleHandler Handler(PaymentsDbContext db)
    {
        var mongo = new Mock<IMongoContext>();
        mongo.Setup(m => m.InsertAuditLogAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return new CreateSaleHandler(db, mongo.Object, NullLogger<CreateSaleHandler>.Instance);
    }

    internal static CreateSaleCommand Cash(string key = "cash-1", int customerId = 0, int sellerId = 1) =>
        new(customerId, sellerId, 1200m, 800m, 100m, "Producto",
            new DateTime(2026, 11, 20), key, true, "Transfer");

    internal static async Task SeedAsync(PaymentsDbContext db)
    {
        db.Sellers.AddRange(new Seller { Id = 1, Name = "Ana" }, new Seller { Id = 2, Name = "Luis" });
        await db.SaveChangesAsync(default);
    }

    [Fact]
    public async Task CashSale_CreatesFullPaymentAndRetryReturnsSameSale()
    {
        using var db = PaymentsContextFactory.Create();
        await SeedAsync(db);
        var handler = Handler(db);
        var command = Cash();
        var id = await handler.Handle(command, default);
        Assert.Equal(id, await handler.Handle(command, default));

        var sale = Assert.Single(await db.Sales.Include(s => s.Payment).Include(s => s.Customer).ToListAsync());
        Assert.True(sale.IsCashSale);
        Assert.True(sale.IsPaid);
        Assert.False(sale.IsCommissionPaid);
        Assert.True(sale.Customer.IsGeneralPublic);
        Assert.Null(sale.Customer.SellerId);
        Assert.Empty(sale.Customer.Phone);
        Assert.Empty(sale.Customer.RFC);
        Assert.Equal(2, sale.Payment.Count);
        var payment = Assert.Single(sale.Payment, p => p.PaymentTypeId == 2);
        Assert.Equal(command.TotalAmount, payment.Amount);
        Assert.Equal(command.Date, payment.PaymentDate);
        Assert.Equal("Transfer", payment.PaymentMethod);

        var dto = Assert.Single(await new GetAllSalesQueryHandler(db).Handle(new GetAllSalesQuery(), default));
        Assert.True(dto.IsCashSale);
        Assert.Equal(1200m, dto.PaidAmount);
        Assert.Equal(0m, dto.RemainingBalance);
        Assert.Equal(100m, dto.PaymentProgress);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(command with { TotalAmount = 1300m }, default));
    }

    [Fact]
    public async Task CashSale_ReusesGeneralCustomerButKeepsSellerOnEachSale()
    {
        using var db = PaymentsContextFactory.Create();
        await SeedAsync(db);
        var handler = Handler(db);
        await handler.Handle(Cash("one"), default);
        await handler.Handle(Cash("two", sellerId: 2), default);

        var customer = Assert.Single(await db.Customers.ToListAsync());
        Assert.True(customer.IsGeneralPublic);
        Assert.Null(customer.SellerId);
        Assert.Equal(new int?[] { 1, 2 }, await db.Sales.OrderBy(s => s.Id).Select(s => s.SellerId).ToArrayAsync());
        Assert.All(await db.Sales.ToListAsync(), s => Assert.Equal(customer.Id, s.CustomerId));
    }

    [Fact]
    public async Task CashSale_WithRealCustomer_DoesNotCreateGeneralCustomer()
    {
        using var db = PaymentsContextFactory.Create();
        await SeedAsync(db);
        db.Customers.Add(new Customer { Id = 10, Name = "Cliente", SellerId = 2 });
        await db.SaveChangesAsync(default);

        await Handler(db).Handle(Cash(customerId: 10), default);

        Assert.False(Assert.Single(await db.Customers.ToListAsync()).IsGeneralPublic);
        Assert.Equal(10, Assert.Single(await db.Sales.ToListAsync()).CustomerId);
    }

    [Fact]
    public async Task CreditSale_PreservesInitialChargeWithoutPayment()
    {
        using var db = PaymentsContextFactory.Create();
        await SeedAsync(db);
        db.Customers.Add(new Customer { Id = 10, Name = "Cliente", SellerId = 1 });
        await db.SaveChangesAsync(default);

        await Handler(db).Handle(Cash(customerId: 10) with { IsCashSale = false, SellerId = null }, default);

        var sale = Assert.Single(await db.Sales.Include(s => s.Payment).ToListAsync());
        Assert.False(sale.IsPaid);
        Assert.False(sale.IsCashSale);
        Assert.Equal(1, Assert.Single(sale.Payment).PaymentTypeId);
    }

    [Fact]
    public async Task CashSale_IsolatesGeneralCustomerAndIdempotencyByTenant()
    {
        var database = $"cash-tenants-{Guid.NewGuid():N}";
        using (var first = PaymentsContextFactory.Create(databaseName: database))
        {
            await SeedAsync(first);
            first.Customers.Add(new Customer { Id = 10, Name = "Privado", SellerId = 1 });
            await first.SaveChangesAsync(default);
            await Handler(first).Handle(Cash(), default);
        }
        using var second = PaymentsContextFactory.Create("other-tenant", database);
        second.Sellers.Add(new Seller { Id = 3, Name = "Otro" });
        await second.SaveChangesAsync(default);
        await Assert.ThrowsAsync<ArgumentException>(() => Handler(second).Handle(Cash(sellerId: 1), default));
        await Assert.ThrowsAsync<ArgumentException>(() => Handler(second).Handle(Cash(customerId: 10, sellerId: 3), default));
        await Handler(second).Handle(Cash(sellerId: 3), default);

        Assert.Equal("other-tenant", Assert.Single(await second.Sales.ToListAsync()).TenantId);
        Assert.Single(await second.Customers.ToListAsync());
        Assert.Equal(2, await second.Customers.IgnoreQueryFilters().CountAsync(c => c.IsGeneralPublic));
    }

    [Fact]
    public void Validator_RequiresCashSellerMethodAndKeyButAllowsAnonymousCustomer()
    {
        var validator = new CreateSaleValidator();
        Assert.True(validator.Validate(Cash()).IsValid);
        Assert.False(validator.Validate(Cash() with { SellerId = null }).IsValid);
        Assert.False(validator.Validate(Cash() with { PaymentMethod = "Other" }).IsValid);
        Assert.False(validator.Validate(Cash() with { IdempotencyKey = null }).IsValid);
        Assert.False(validator.Validate(Cash() with { CustomerId = -1 }).IsValid);
        Assert.False(validator.Validate(Cash() with { IsCashSale = false }).IsValid);
        Assert.False(validator.Validate(Cash() with { TotalAmount = 1200.001m }).IsValid);
    }

    [Fact]
    public async Task GeneralCustomer_IsNotAccessibleThroughPublicHistory()
    {
        using var db = PaymentsContextFactory.Create();
        await SeedAsync(db);
        db.Tenants.Add(new Tenant { Id = PaymentsContextFactory.TenantId, Name = "Empresa", IsActive = true });
        await db.SaveChangesAsync(default);
        await Handler(db).Handle(Cash(), default);

        var result = await new GetPublicHistoryQueryHandler(db).Handle(
            new GetPublicHistoryQuery("", "", PaymentsContextFactory.TenantId), default);

        Assert.False(result.CustomerFound);
    }

    [Fact]
    public async Task CashSale_RejectsChangesThatWouldBreakSettlement()
    {
        using var db = PaymentsContextFactory.Create();
        await SeedAsync(db);
        var id = await Handler(db).Handle(Cash(), default);
        var sale = await db.Sales.Include(s => s.Payment).SingleAsync();
        var payment = sale.Payment.Single(p => p.PaymentTypeId == 2);

        var user = new Mock<ICurrentUserService>();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new DeletePaymentCommandHandler(db, user.Object).Handle(new DeletePaymentCommand(payment.Id), default));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new UpdatePaymentHandler(db).Handle(new UpdatePaymentCommand(payment.Id, 10m, "Cash", null, null), default));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new UpdateSaleHandler(db).Handle(
                new UpdateSaleCommand(id, sale.CustomerId, 1, 1300m, 800m, 100m, "Producto", sale.Date), default));

        var register = new RegisterPaymentHandler(db, Mock.Of<IMongoContext>(), Mock.Of<ICacheService>(),
            NullLogger<RegisterPaymentHandler>.Instance);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            register.Handle(new RegisterPaymentCommand(id, 10m, "Cash", sale.Date), default));
        Assert.Equal(1200m, (await db.Payments.SingleAsync(p => p.PaymentTypeId == 2)).Amount);
    }
}
