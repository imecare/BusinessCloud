using BusinessCloud.Application.Bazarez.Queries.SearchClosureCustomerTotals;
using BusinessCloud.Domain.Bazares.Entities;
using BusinessCloud.Tests.TestSupport;
using Xunit;

namespace BusinessCloud.Tests.Application.Bazares;

public class SearchClosureCustomerTotalsHandlerTests
{
    private const string Tenant = BazaresContextFactory.TenantId;

    private static BzaClosureCustomerTotal Total(int id, BzaCustomer customer, decimal amount) => new()
    {
        Id = id,
        TenantId = Tenant,
        BzaCustomerId = customer.Id,
        Customer = customer,
        TotalAmount = amount,
        UploadToken = $"tok-{id}",
        Status = BzaClosureCustomerTotalStatus.Pending,
    };

    [Fact]
    public async Task Handle_PorNombre_AgrupaSoloLosTotalesDelClienteEnCadaCierre()
    {
        using var ctx = BazaresContextFactory.Create();
        var ana = new BzaCustomer { Id = 1, TenantId = Tenant, Name = "Ana Lopez", Phone = "5511112222" };
        var beto = new BzaCustomer { Id = 2, TenantId = Tenant, Name = "Beto Ruiz", Phone = "5513334444" };
        ctx.Customers.AddRange(ana, beto);

        ctx.ClosureEvents.Add(new BzaClosureEvent
        {
            Id = 10,
            TenantId = Tenant,
            Description = "Cierre A",
            PaymentDeadline = DateTime.UtcNow.AddDays(3),
            CustomerTotals = new List<BzaClosureCustomerTotal> { Total(100, ana, 320m), Total(101, beto, 150m) },
        });
        ctx.ClosureEvents.Add(new BzaClosureEvent
        {
            Id = 20,
            TenantId = Tenant,
            Description = "Cierre B",
            PaymentDeadline = DateTime.UtcNow.AddDays(5),
            CustomerTotals = new List<BzaClosureCustomerTotal> { Total(102, ana, 500m) },
        });
        await ctx.SaveChangesAsync(default);

        var handler = new SearchClosureCustomerTotalsHandler(ctx);
        var result = await handler.Handle(new SearchClosureCustomerTotalsQuery("ana"), default);

        Assert.Equal(2, result.Count);
        Assert.Equal(new[] { 10, 20 }, result.Select(g => g.ClosureEventId).OrderBy(x => x).ToArray());
        Assert.All(result, g => Assert.All(g.Customers, c => Assert.Contains("Ana", c.CustomerName)));
        // Beto no debe aparecer en ningún grupo.
        Assert.DoesNotContain(result.SelectMany(g => g.Customers), c => c.CustomerName.Contains("Beto"));
    }

    [Fact]
    public async Task Handle_PorTelefono_DevuelveSoloElClienteQueCoincide()
    {
        using var ctx = BazaresContextFactory.Create();
        var ana = new BzaCustomer { Id = 1, TenantId = Tenant, Name = "Ana Lopez", Phone = "5511112222" };
        var beto = new BzaCustomer { Id = 2, TenantId = Tenant, Name = "Beto Ruiz", Phone = "5513334444" };
        ctx.Customers.AddRange(ana, beto);
        ctx.ClosureEvents.Add(new BzaClosureEvent
        {
            Id = 10,
            TenantId = Tenant,
            Description = "Cierre A",
            PaymentDeadline = DateTime.UtcNow.AddDays(3),
            CustomerTotals = new List<BzaClosureCustomerTotal> { Total(100, ana, 320m), Total(101, beto, 150m) },
        });
        await ctx.SaveChangesAsync(default);

        var handler = new SearchClosureCustomerTotalsHandler(ctx);
        var result = await handler.Handle(new SearchClosureCustomerTotalsQuery("3334"), default);

        var group = Assert.Single(result);
        var customer = Assert.Single(group.Customers);
        Assert.Equal("Beto Ruiz", customer.CustomerName);
    }

    [Fact]
    public async Task Handle_QueryVacio_DevuelveVacio()
    {
        using var ctx = BazaresContextFactory.Create();
        var handler = new SearchClosureCustomerTotalsHandler(ctx);

        var result = await handler.Handle(new SearchClosureCustomerTotalsQuery("   "), default);

        Assert.Empty(result);
    }
}
