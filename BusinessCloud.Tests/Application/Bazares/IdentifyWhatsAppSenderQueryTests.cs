using BusinessCloud.Application.Bazares.Queries.IdentifyWhatsAppSender;
using BusinessCloud.Domain.Bazares.Entities;
using BusinessCloud.Tests.TestSupport;
using Xunit;

namespace BusinessCloud.Tests.Application.Bazares;

public class IdentifyWhatsAppSenderQueryTests
{
    private const string TenantA = "tenant-a";

    [Fact]
    public async Task Handle_TelefonoDeCliente_DevuelveCuentasActivas()
    {
        using var bazares = BazaresContextFactory.Create();
        bazares.BazarSettings.Add(new BzaBazarSettings
        {
            Id = 1,
            TenantId = TenantA,
            BazarName = "Bazar Uno",
            SalesWhatsApp = "+52 55 1234 5678",
        });
        bazares.ClosureCustomerTotals.Add(new BzaClosureCustomerTotal
        {
            Id = 1,
            TenantId = TenantA,
            BzaClosureEventId = 1,
            BzaCustomerId = 1,
            TotalAmount = 320m,
            UploadToken = "tok-1",
            Status = BzaClosureCustomerTotalStatus.Pending,
            Customer = new BzaCustomer
            {
                Id = 1,
                TenantId = TenantA,
                Name = "Ana",
                Phone = "525511112222",
            },
        });
        await bazares.SaveChangesAsync(default);
        var handler = new IdentifyWhatsAppSenderHandler(bazares);

        var result = await handler.Handle(new IdentifyWhatsAppSenderQuery("5511112222"), default);

        Assert.Equal(WhatsAppSenderRole.Customer, result.Role);
        var account = Assert.Single(result.CustomerAccounts);
        Assert.Equal("tok-1", account.UploadToken);
        Assert.Equal("+52 55 1234 5678", account.BazarWhatsApp);
    }

    [Fact]
    public async Task Handle_ClienteSinAdeudoActivo_ConservaRolClienteSinCuentas()
    {
        using var bazares = BazaresContextFactory.Create();
        bazares.Customers.Add(new BzaCustomer
        {
            Id = 1,
            TenantId = TenantA,
            Name = "Ana",
            Phone = "5215511112222",
        });
        await bazares.SaveChangesAsync(default);
        var handler = new IdentifyWhatsAppSenderHandler(bazares);

        var result = await handler.Handle(new IdentifyWhatsAppSenderQuery("5215511112222"), default);

        Assert.Equal(WhatsAppSenderRole.Customer, result.Role);
        Assert.Empty(result.CustomerAccounts);
    }

    [Fact]
    public async Task Handle_TelefonoDesconocido_DevuelveRolUnknown()
    {
        using var bazares = BazaresContextFactory.Create();
        var handler = new IdentifyWhatsAppSenderHandler(bazares);

        var result = await handler.Handle(new IdentifyWhatsAppSenderQuery("5599999999"), default);

        Assert.Equal(WhatsAppSenderRole.Unknown, result.Role);
        Assert.Empty(result.CustomerAccounts);
    }

    [Fact]
    public async Task Handle_ClienteConCuentaValidada_NoIncluyeLaCuenta()
    {
        using var bazares = BazaresContextFactory.Create();
        bazares.ClosureCustomerTotals.Add(new BzaClosureCustomerTotal
        {
            Id = 1,
            TenantId = TenantA,
            BzaClosureEventId = 1,
            BzaCustomerId = 1,
            TotalAmount = 50m,
            UploadToken = "tok-validado",
            Status = BzaClosureCustomerTotalStatus.Validated,
            Customer = new BzaCustomer
            {
                Id = 1,
                TenantId = TenantA,
                Name = "Ana",
                Phone = "5215511112222",
            },
        });
        await bazares.SaveChangesAsync(default);
        var handler = new IdentifyWhatsAppSenderHandler(bazares);

        var result = await handler.Handle(new IdentifyWhatsAppSenderQuery("5215511112222"), default);

        Assert.Equal(WhatsAppSenderRole.Customer, result.Role);
        Assert.Empty(result.CustomerAccounts);
    }
}