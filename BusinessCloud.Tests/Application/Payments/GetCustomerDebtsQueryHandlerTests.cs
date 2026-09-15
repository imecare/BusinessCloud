using BusinessCloud.Application.Payments.Queries.GetCustomerDebts;
using BusinessCloud.Domain.Payments.Entities;
using BusinessCloud.Tests.TestSupport;
using Xunit;

namespace BusinessCloud.Tests.Application.Payments;

public sealed class GetCustomerDebtsQueryHandlerTests
{
    private const string TenantId = PaymentsContextFactory.TenantId;

    [Fact]
    public async Task Handle_AgrupaSoloClientesConDeuda_YOrdenaPorUltimoAbono()
    {
        await using var context = PaymentsContextFactory.Create();
        var seller = new Seller
        {
            Id = 1,
            TenantId = TenantId,
            Name = "Imelda",
            LastName = "Peña"
        };
        context.Sellers.Add(seller);
        context.Customers.AddRange(
            new Customer { Id = 1, TenantId = TenantId, Name = "Ana", LastName = "López", SellerId = 1 },
            new Customer { Id = 2, TenantId = TenantId, Name = "Beatriz", LastName = "Ruiz", SellerId = 1 },
            new Customer { Id = 3, TenantId = TenantId, Name = "Carla", LastName = "Díaz", SellerId = 1 });
        context.Sales.AddRange(
            new Sale { Id = 1, TenantId = TenantId, CustomerId = 1, SellerId = 1, TotalAmount = 500m },
            new Sale { Id = 2, TenantId = TenantId, CustomerId = 1, SellerId = 1, TotalAmount = 300m },
            new Sale { Id = 3, TenantId = TenantId, CustomerId = 2, SellerId = 1, TotalAmount = 400m },
            new Sale { Id = 4, TenantId = TenantId, CustomerId = 3, SellerId = 1, TotalAmount = 200m, IsPaid = true });
        context.Payments.AddRange(
            new Payment
            {
                Id = 1,
                TenantId = TenantId,
                SaleId = 1,
                Amount = 100m,
                PaymentTypeId = 2,
                PaymentDate = new DateTime(2026, 8, 1)
            },
            new Payment
            {
                Id = 2,
                TenantId = TenantId,
                SaleId = 2,
                Amount = 50m,
                PaymentTypeId = 2,
                PaymentDate = new DateTime(2026, 9, 1)
            },
            new Payment
            {
                Id = 3,
                TenantId = TenantId,
                SaleId = 4,
                Amount = 200m,
                PaymentTypeId = 2,
                PaymentDate = new DateTime(2026, 7, 1)
            });
        await context.SaveChangesAsync();

        var handler = new GetCustomerDebtsQueryHandler(context);

        var result = await handler.Handle(new GetCustomerDebtsQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal(2, result[0].CustomerId);
        Assert.Null(result[0].LastPaymentDate);
        Assert.Equal(1, result[1].CustomerId);
        Assert.Equal(800m, result[1].TotalAmount);
        Assert.Equal(150m, result[1].PaidAmount);
        Assert.Equal(650m, result[1].DebtAmount);
        Assert.Equal(2, result[1].PendingSalesCount);
        Assert.Equal(new DateTime(2026, 9, 1), result[1].LastPaymentDate);
        Assert.DoesNotContain(result, debt => debt.CustomerId == 3);
    }
}