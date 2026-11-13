 using BusinessCloud.Application.Payments.Queries.GetPublicHistory;
using BusinessCloud.Domain.Common.Entities;
using BusinessCloud.Domain.Payments.Entities;
using BusinessCloud.Tests.TestSupport;
using Xunit;

namespace BusinessCloud.Tests.Application.Payments;

public class GetPublicHistoryQueryHandlerTests
{
    [Fact]
    public async Task Handle_CalculatesTotalsOnlyFromActivePurchases()
    {
        await using var context = PaymentsContextFactory.Create();
        context.Tenants.Add(new Tenant
        {
            Id = PaymentsContextFactory.TenantId,
            Name = "Empresa",
            IsActive = true
        });
        context.Customers.Add(new Customer
        {
            Id = 1,
            TenantId = PaymentsContextFactory.TenantId,
            Name = "Maria",
            LastName = "Alba",
            Phone = "5512345678",
            RFC = "AABC800101ABC"
        });
        context.Sales.AddRange(
            new Sale
            {
                Id = 1,
                TenantId = PaymentsContextFactory.TenantId,
                CustomerId = 1,
                TotalAmount = 1_000m,
                IsPaid = false,
                Date = new DateTime(2026, 10, 1)
            },
            new Sale
            {
                Id = 2,
                TenantId = PaymentsContextFactory.TenantId,
                CustomerId = 1,
                TotalAmount = 500m,
                IsPaid = true,
                Date = new DateTime(2026, 9, 1)
            });
        context.Payments.AddRange(
            new Payment
            {
                Id = 1,
                TenantId = PaymentsContextFactory.TenantId,
                SaleId = 1,
                Amount = 400m,
                PaymentTypeId = 2
            },
            new Payment
            {
                Id = 2,
                TenantId = PaymentsContextFactory.TenantId,
                SaleId = 1,
                Amount = 100m,
                PaymentTypeId = 1
            },
            new Payment
            {
                Id = 3,
                TenantId = PaymentsContextFactory.TenantId,
                SaleId = 2,
                Amount = 500m,
                PaymentTypeId = 2
            });
        await context.SaveChangesAsync();

        var handler = new GetPublicHistoryQueryHandler(context);

        var result = await handler.Handle(
            new GetPublicHistoryQuery("5512345678", "AABC800101ABC", PaymentsContextFactory.TenantId),
            CancellationToken.None);

        Assert.True(result.CustomerFound);
        Assert.NotNull(result.Data);
        Assert.Equal(1_000m, result.Data.TotalActivePurchases);
        Assert.Equal(400m, result.Data.TotalPaidOnActivePurchases);
        Assert.Equal(2, result.Data.Sales.Count);
    }
}
