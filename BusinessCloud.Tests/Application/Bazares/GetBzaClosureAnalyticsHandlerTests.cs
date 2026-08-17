using BusinessCloud.Application.Bazares.Queries.GetBzaClosureAnalytics;
using BusinessCloud.Domain.Bazares.Entities;
using BusinessCloud.Tests.TestSupport;
using Xunit;

namespace BusinessCloud.Tests.Application.Bazares;

public class GetBzaClosureAnalyticsHandlerTests
{
    private const string Tenant = BazaresContextFactory.TenantId;

    [Fact]
    public async Task Handle_CalculaMetricasPorEvento_YExcluyeTotalesCancelados()
    {
        using var context = BazaresContextFactory.Create();
        var customer = CreateCustomer(1);
        var closure = CreateClosure(10, "Cierre agosto");
        closure.CustomerTotals =
        [
            CreateTotal(100, customer, 100m, BzaClosureCustomerTotalStatus.Validated),
            CreateTotal(101, customer, 50m, BzaClosureCustomerTotalStatus.Pending),
            CreateTotal(102, customer, 999m, BzaClosureCustomerTotalStatus.Cancelled),
        ];

        context.Customers.Add(customer);
        context.ClosureEvents.Add(closure);
        context.Sales.Add(CreateSale(500, customer, closure.Id, 2));
        await context.SaveChangesAsync(default);
        closure.CreatedAt = new DateTime(2026, 8, 12, 10, 0, 0, DateTimeKind.Utc);
        await context.SaveChangesAsync(default);

        var result = await new GetBzaClosureAnalyticsHandler(context)
            .Handle(new GetBzaClosureAnalyticsQuery(2026), default);

        var metric = Assert.Single(result.PerEvent);
        Assert.Equal(2, metric.ProductCount);
        Assert.Equal(150m, metric.TotalSales);
        Assert.Equal(100m, metric.TotalPaid);
        Assert.Equal(50m, metric.TotalUnpaid);
    }

    [Fact]
    public async Task Handle_ExcluyeCierresCancelados()
    {
        using var context = BazaresContextFactory.Create();
        var customer = CreateCustomer(1);
        var active = CreateClosure(10, "Activo");
        var cancelled = CreateClosure(20, "Cancelado", BzaClosureEventStatus.Cancelled);
        active.CustomerTotals = [CreateTotal(100, customer, 80m, BzaClosureCustomerTotalStatus.Pending)];
        cancelled.CustomerTotals = [CreateTotal(200, customer, 500m, BzaClosureCustomerTotalStatus.Validated)];

        context.Customers.Add(customer);
        context.ClosureEvents.AddRange(active, cancelled);
        context.Sales.AddRange(
            CreateSale(500, customer, active.Id, 1),
            CreateSale(600, customer, cancelled.Id, 3));
        await context.SaveChangesAsync(default);
        active.CreatedAt = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        cancelled.CreatedAt = new DateTime(2026, 7, 2, 0, 0, 0, DateTimeKind.Utc);
        await context.SaveChangesAsync(default);

        var result = await new GetBzaClosureAnalyticsHandler(context)
            .Handle(new GetBzaClosureAnalyticsQuery(2026), default);

        var metric = Assert.Single(result.PerEvent);
        Assert.Equal(active.Id, metric.ClosureEventId);
        var july = Assert.Single(result.PerMonth, month => month.Month == 7);
        Assert.Equal(1, july.ProductCount);
        Assert.Equal(80m, july.TotalSales);
    }

    [Fact]
    public async Task Handle_AgrupaPorMes_EIncluyeLosDoceMesesEnOrden()
    {
        using var context = BazaresContextFactory.Create();
        var customer = CreateCustomer(1);
        var firstAugust = CreateClosure(10, "Agosto A");
        var secondAugust = CreateClosure(20, "Agosto B");
        var september = CreateClosure(30, "Septiembre");
        firstAugust.CustomerTotals = [CreateTotal(100, customer, 100m, BzaClosureCustomerTotalStatus.Validated)];
        secondAugust.CustomerTotals = [CreateTotal(200, customer, 40m, BzaClosureCustomerTotalStatus.Pending)];
        september.CustomerTotals = [CreateTotal(300, customer, 60m, BzaClosureCustomerTotalStatus.Validated)];

        context.Customers.Add(customer);
        context.ClosureEvents.AddRange(firstAugust, secondAugust, september);
        context.Sales.AddRange(
            CreateSale(500, customer, firstAugust.Id, 1),
            CreateSale(600, customer, secondAugust.Id, 2),
            CreateSale(700, customer, september.Id, 1));
        await context.SaveChangesAsync(default);
        firstAugust.CreatedAt = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        secondAugust.CreatedAt = new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc);
        september.CreatedAt = new DateTime(2026, 9, 3, 0, 0, 0, DateTimeKind.Utc);
        await context.SaveChangesAsync(default);

        var result = await new GetBzaClosureAnalyticsHandler(context)
            .Handle(new GetBzaClosureAnalyticsQuery(2026), default);

        Assert.Equal(12, result.PerMonth.Count);
        Assert.Equal(Enumerable.Range(1, 12), result.PerMonth.Select(month => month.Month));
        var august = result.PerMonth.Single(month => month.Month == 8);
        Assert.Equal("Ago 2026", august.Label);
        Assert.Equal(3, august.ProductCount);
        Assert.Equal(140m, august.TotalSales);
        Assert.Equal(100m, august.TotalPaid);
        Assert.Equal(40m, august.TotalUnpaid);
        Assert.Equal([2026], result.AvailableYears);
    }

    private static BzaCustomer CreateCustomer(int id) => new()
    {
        Id = id,
        TenantId = Tenant,
        Name = "Cliente prueba",
        Phone = "5512345678",
    };

    private static BzaClosureEvent CreateClosure(
        int id,
        string description,
        int status = BzaClosureEventStatus.PendingPayment) => new()
    {
        Id = id,
        TenantId = Tenant,
        Description = description,
        PaymentDeadline = DateTime.UtcNow.AddDays(5),
        Status = status,
    };

    private static BzaClosureCustomerTotal CreateTotal(
        int id,
        BzaCustomer customer,
        decimal amount,
        int status) => new()
    {
        Id = id,
        TenantId = Tenant,
        BzaCustomerId = customer.Id,
        Customer = customer,
        TotalAmount = amount,
        UploadToken = $"token-{id}",
        Status = status,
    };

    private static BzaSale CreateSale(
        int id,
        BzaCustomer customer,
        int closureId,
        int productCount) => new()
    {
        Id = id,
        TenantId = Tenant,
        BzaEventId = id,
        BzaCustomerId = customer.Id,
        Customer = customer,
        BzaClosureEventId = closureId,
        Products = Enumerable.Range(1, productCount)
            .Select(index => new BzaSoldProduct
            {
                Id = id * 10 + index,
                TenantId = Tenant,
                Description = $"Producto {index}",
                Price = 10m,
            })
            .ToList(),
    };
}
