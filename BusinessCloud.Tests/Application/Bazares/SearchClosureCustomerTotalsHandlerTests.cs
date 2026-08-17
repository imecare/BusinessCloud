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

    [Fact]
    public async Task Handle_UsaSiempreFormatoV4ConProductos()
    {
        using var ctx = BazaresContextFactory.Create();
        ctx.BazarSettings.Add(new BzaBazarSettings { Id = 1, TenantId = Tenant, BazarName = "Bazar Test" });
        var ana = new BzaCustomer { Id = 1, TenantId = Tenant, Name = "Ana Lopez", Phone = "5511112222" };
        ctx.Customers.Add(ana);
        ctx.ClosureEvents.Add(new BzaClosureEvent
        {
            Id = 10,
            TenantId = Tenant,
            Description = "Cierre semanal",
            PaymentDeadline = DateTime.UtcNow.AddDays(3),
            CustomerTotals = new List<BzaClosureCustomerTotal> { Total(100, ana, 320m) },
        });
        ctx.Sales.Add(new BzaSale
        {
            Id = 500,
            TenantId = Tenant,
            BzaEventId = 1,
            BzaCustomerId = ana.Id,
            BzaClosureEventId = 10,
            Products =
            [
                new BzaSoldProduct { Id = 900, TenantId = Tenant, Description = "Blusa", Price = 120m },
                new BzaSoldProduct { Id = 901, TenantId = Tenant, Description = "Bolsa", Price = 200m },
            ],
        });
        await ctx.SaveChangesAsync(default);

        var handler = new SearchClosureCustomerTotalsHandler(ctx);
        var result = await handler.Handle(new SearchClosureCustomerTotalsQuery("ana"), default);

        var message = Assert.Single(Assert.Single(result).Customers).Message;
        // El mensaje que se copia a memoria SIEMPRE usa el formato v4 (enlace en lugar de botón),
        // sin depender del setting de plantilla.
        Assert.Contains("Aviso de pago de Bazar Test (mensaje automático)", message);
        Assert.Contains("*Total de producto(s) · 2* - (Blusa, Bolsa)", message);
        Assert.Contains("__UPLOAD_LINK__", message);
        Assert.DoesNotContain("Para consultar las tarjetas de pago, subir tu comprobante", message);
    }

    [Fact]
    public async Task Handle_SinProductos_UsaFormatoV4ConGuion()
    {
        using var ctx = BazaresContextFactory.Create();
        ctx.BazarSettings.Add(new BzaBazarSettings { Id = 1, TenantId = Tenant, BazarName = "Bazar Test" });
        var ana = new BzaCustomer { Id = 1, TenantId = Tenant, Name = "Ana Lopez", Phone = "5511112222" };
        ctx.Customers.Add(ana);
        ctx.ClosureEvents.Add(new BzaClosureEvent
        {
            Id = 10,
            TenantId = Tenant,
            Description = "Cierre semanal",
            PaymentDeadline = DateTime.UtcNow.AddDays(3),
            CustomerTotals = new List<BzaClosureCustomerTotal> { Total(100, ana, 320m) },
        });
        await ctx.SaveChangesAsync(default);

        var handler = new SearchClosureCustomerTotalsHandler(ctx);
        var result = await handler.Handle(new SearchClosureCustomerTotalsQuery("ana"), default);

        var message = Assert.Single(Assert.Single(result).Customers).Message;
        // Aun sin config ni productos, el copy-to-memory es v4 (fallback "—" para productos).
        Assert.Contains("Aviso de pago de Bazar Test (mensaje automático)", message);
        Assert.Contains("*Total de producto(s) · 0* - (—)", message);
        Assert.DoesNotContain("Para consultar las tarjetas de pago, subir tu comprobante", message);
    }
}
