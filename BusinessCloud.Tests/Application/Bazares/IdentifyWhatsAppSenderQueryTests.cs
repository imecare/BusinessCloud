using BusinessCloud.Application.Bazares.Queries.IdentifyWhatsAppSender;
using BusinessCloud.Domain.Bazares.Entities;
using BusinessCloud.Domain.Common.Entities;
using BusinessCloud.Tests.TestSupport;
using Xunit;

namespace BusinessCloud.Tests.Application.Bazares;

/// <summary>
/// Pruebas de la identificación del remitente de WhatsApp por teléfono: prioridad de rol
/// (Dueño sobre Cliente), normalización del número y filtrado de cuentas activas.
/// </summary>
public class IdentifyWhatsAppSenderQueryTests
{
    private const string TenantA = "tenant-a";
    private const string TenantB = "tenant-b";

    [Fact]
    public async Task Handle_TelefonoDeCliente_DevuelveRolClienteConCuentasActivas()
    {
        using var identity = IdentityContextFactory.Create();
        using var bazares = BazaresContextFactory.Create();

        bazares.BazarSettings.Add(new BzaBazarSettings { Id = 1, TenantId = TenantA, BazarName = "Bazar Uno" });
        bazares.ClosureCustomerTotals.Add(new BzaClosureCustomerTotal
        {
            Id = 1,
            TenantId = TenantA,
            BzaClosureEventId = 1,
            BzaCustomerId = 1,
            TotalAmount = 320m,
            UploadToken = "tok-1",
            Status = BzaClosureCustomerTotalStatus.Pending,
            Customer = new BzaCustomer { Id = 1, TenantId = TenantA, Name = "Ana", Phone = "525511112222" },
        });
        await bazares.SaveChangesAsync(default);

        var handler = new IdentifyWhatsAppSenderHandler(identity, bazares);
        var result = await handler.Handle(new IdentifyWhatsAppSenderQuery("5511112222"), default);

        Assert.Equal(WhatsAppSenderRole.Customer, result.Role);
        Assert.Single(result.CustomerAccounts);
        Assert.Equal("tok-1", result.CustomerAccounts[0].UploadToken);
    }

    [Fact]
    public async Task Handle_TelefonoDeDuenoYCliente_PriorizaRolDueno()
    {
        using var identity = IdentityContextFactory.Create();
        using var bazares = BazaresContextFactory.Create();

        identity.Tenants.Add(new Tenant { Id = TenantA, Name = "Empresa A" });
        identity.TenantSubscriptions.Add(new TenantSubscription
        {
            Id = 1,
            TenantId = TenantA,
            OwnerPhone = "5215511112222",
            PaidUntil = DateTime.UtcNow.AddMonths(1),
        });
        await identity.SaveChangesAsync(default);

        bazares.BazarSettings.Add(new BzaBazarSettings { Id = 1, TenantId = TenantA, BazarName = "Bazar Uno" });
        bazares.ClosureCustomerTotals.Add(new BzaClosureCustomerTotal
        {
            Id = 1,
            TenantId = TenantB,
            BzaClosureEventId = 1,
            BzaCustomerId = 2,
            TotalAmount = 100m,
            UploadToken = "tok-2",
            Status = BzaClosureCustomerTotalStatus.Rejected,
            Customer = new BzaCustomer { Id = 2, TenantId = TenantB, Name = "Ana", Phone = "5215511112222" },
        });
        await bazares.SaveChangesAsync(default);

        var handler = new IdentifyWhatsAppSenderHandler(identity, bazares);
        var result = await handler.Handle(new IdentifyWhatsAppSenderQuery("5215511112222"), default);

        Assert.Equal(WhatsAppSenderRole.Owner, result.Role);
        Assert.Single(result.OwnerTenants);
        Assert.Equal(TenantA, result.OwnerTenants[0].TenantId);
    }

    [Fact]
    public async Task Handle_TelefonoDesconocido_DevuelveRolUnknown()
    {
        using var identity = IdentityContextFactory.Create();
        using var bazares = BazaresContextFactory.Create();

        var handler = new IdentifyWhatsAppSenderHandler(identity, bazares);
        var result = await handler.Handle(new IdentifyWhatsAppSenderQuery("5599999999"), default);

        Assert.Equal(WhatsAppSenderRole.Unknown, result.Role);
        Assert.Empty(result.OwnerTenants);
        Assert.Empty(result.CustomerAccounts);
    }

    [Fact]
    public async Task Handle_ClienteConCuentaValidada_NoLaIncluye()
    {
        using var identity = IdentityContextFactory.Create();
        using var bazares = BazaresContextFactory.Create();

        bazares.BazarSettings.Add(new BzaBazarSettings { Id = 1, TenantId = TenantA, BazarName = "Bazar Uno" });
        bazares.ClosureCustomerTotals.Add(new BzaClosureCustomerTotal
        {
            Id = 1,
            TenantId = TenantA,
            BzaClosureEventId = 1,
            BzaCustomerId = 1,
            TotalAmount = 50m,
            UploadToken = "tok-validado",
            Status = BzaClosureCustomerTotalStatus.Validated,
            Customer = new BzaCustomer { Id = 1, TenantId = TenantA, Name = "Ana", Phone = "5215511112222" },
        });
        await bazares.SaveChangesAsync(default);

        var handler = new IdentifyWhatsAppSenderHandler(identity, bazares);
        var result = await handler.Handle(new IdentifyWhatsAppSenderQuery("5215511112222"), default);

        Assert.Equal(WhatsAppSenderRole.Unknown, result.Role);
        Assert.Empty(result.CustomerAccounts);
    }
}
