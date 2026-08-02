using BusinessCloud.Application.Bazares.Commands.MarkCustomerNotificationRead;
using BusinessCloud.Application.Bazares.Commands.SendClosureWhatsApp;
using BusinessCloud.Application.Bazares.Queries.GetClosureWhatsAppStatus;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using BusinessCloud.Infrastructure.Data;
using BusinessCloud.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace BusinessCloud.Tests.Application.Bazares;

public class CustomerInboxNotificationTests
{
    private const string Tenant = BazaresContextFactory.TenantId;

    [Fact]
    public async Task SendClosureWhatsApp_CreatesSingleInboxNotificationAcrossRetries()
    {
        using var context = BazaresContextFactory.Create();
        var customer = new BzaCustomer
        {
            Id = 1,
            TenantId = Tenant,
            Name = "Cliente Uno",
            Phone = "5511112222",
            PortalToken = "portal-token",
        };
        var total = new BzaClosureCustomerTotal
        {
            Id = 10,
            TenantId = Tenant,
            BzaCustomerId = customer.Id,
            Customer = customer,
            TotalAmount = 450m,
            UploadToken = "upload-token",
        };
        context.BazarSettings.Add(new BzaBazarSettings { Id = 1, TenantId = Tenant, BazarName = "Bazar Test" });
        context.ClosureEvents.Add(new BzaClosureEvent
        {
            Id = 20,
            TenantId = Tenant,
            Description = "Cierre semanal",
            PaymentDeadline = DateTime.UtcNow.AddDays(2),
            CustomerTotals = [total],
        });
        await context.SaveChangesAsync(default);

        var identityOptions = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"identity-{Guid.NewGuid():N}")
            .Options;
        await using var identityContext = new IdentityDbContext(identityOptions);
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.TenantId).Returns(Tenant);
        var whatsApp = new Mock<IWhatsAppSender>();
        whatsApp.Setup(x => x.SendTemplateWithResultAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>(),
                It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(new WhatsAppSendResult(true, "wamid-1", null, null));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["WhatsApp:ClosureTotalsTemplateName"] = "total_compra_v3",
            ["WhatsApp:ClosureTotalsTemplateLang"] = "es",
        }).Build();
        var handler = new SendClosureWhatsAppHandler(
            context, whatsApp.Object, identityContext, currentUser.Object, configuration);

        var first = await handler.Handle(new SendClosureWhatsAppCommand(20, "https://portal.test"), default);
        var second = await handler.Handle(new SendClosureWhatsAppCommand(20, "https://portal.test", [1]), default);

        var notification = Assert.Single(context.CustomerInboxNotifications);
        Assert.Equal("/comprobante/upload-token", notification.ActionUrl);
        Assert.Contains("$450.00", notification.Message);
        Assert.True(Assert.Single(first.Items).InboxNotificationCreated);
        Assert.True(Assert.Single(second.Items).InboxNotificationCreated);
    }

    [Fact]
    public async Task SendClosureWhatsApp_IncludesConfiguredCutoffTimeInDeadline()
    {
        using var context = BazaresContextFactory.Create();
        var customer = new BzaCustomer
        {
            Id = 1,
            TenantId = Tenant,
            Name = "Cliente Uno",
            Phone = "5511112222",
        };
        var total = new BzaClosureCustomerTotal
        {
            Id = 10,
            TenantId = Tenant,
            BzaCustomerId = customer.Id,
            Customer = customer,
            TotalAmount = 450m,
            UploadToken = "upload-token",
        };
        context.BazarSettings.Add(new BzaBazarSettings
        {
            Id = 1,
            TenantId = Tenant,
            BazarName = "Bazar Test",
            PaymentCutoffTime = "18:30",
        });
        context.ClosureEvents.Add(new BzaClosureEvent
        {
            Id = 20,
            TenantId = Tenant,
            Description = "Cierre semanal",
            PaymentDeadline = new DateTime(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc),
            CustomerTotals = [total],
        });
        await context.SaveChangesAsync(default);

        var identityOptions = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"identity-{Guid.NewGuid():N}")
            .Options;
        await using var identityContext = new IdentityDbContext(identityOptions);
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.TenantId).Returns(Tenant);

        IReadOnlyList<string>? capturedBody = null;
        var whatsApp = new Mock<IWhatsAppSender>();
        whatsApp.Setup(x => x.SendTemplateWithResultAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>(),
                It.IsAny<string?>(), It.IsAny<string?>()))
            .Callback<string, string, string, IReadOnlyList<string>, CancellationToken, string?, string?>(
                (_, _, _, body, _, _, _) => capturedBody = body)
            .ReturnsAsync(new WhatsAppSendResult(true, "wamid-1", null, null));

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["WhatsApp:ClosureTotalsTemplateName"] = "total_compra_v3",
            ["WhatsApp:ClosureTotalsTemplateLang"] = "es",
        }).Build();
        var handler = new SendClosureWhatsAppHandler(
            context, whatsApp.Object, identityContext, currentUser.Object, configuration);

        await handler.Handle(new SendClosureWhatsAppCommand(20, "https://portal.test"), default);

        Assert.NotNull(capturedBody);
        // El 4o parametro del cuerpo es la fecha limite; debe incluir la hora configurada (18:30 -> 06:30 p. m.).
        var deadlineParam = capturedBody![3];
        Assert.Contains("a las", deadlineParam);
        Assert.Contains("06:30", deadlineParam);
    }

    [Fact]
    public async Task GetClosureWhatsAppStatus_SentForFifteenMinutes_IsUnconfirmed()
    {
        using var context = BazaresContextFactory.Create();
        var customer = new BzaCustomer { Id = 1, TenantId = Tenant, Name = "Cliente Uno", Phone = "5511112222" };
        var total = new BzaClosureCustomerTotal
        {
            Id = 10,
            TenantId = Tenant,
            BzaCustomerId = 1,
            Customer = customer,
            UploadToken = "upload-token",
        };
        context.ClosureEvents.Add(new BzaClosureEvent
        {
            Id = 20,
            TenantId = Tenant,
            Description = "Cierre",
            PaymentDeadline = DateTime.UtcNow.AddDays(1),
            CustomerTotals = [total],
        });
        context.WhatsAppMessages.Add(new BzaWhatsAppMessage
        {
            TenantId = Tenant,
            Purpose = "totals",
            BzaCustomerId = 1,
            BzaClosureCustomerTotalId = 10,
            Status = "sent",
            SentAt = DateTime.UtcNow.AddMinutes(-16),
        });
        context.CustomerInboxNotifications.Add(new BzaCustomerInboxNotification
        {
            TenantId = Tenant,
            BzaCustomerId = 1,
            BzaClosureCustomerTotalId = 10,
            Title = "Total",
            Message = "Mensaje",
            ActionUrl = "/comprobante/upload-token",
        });
        await context.SaveChangesAsync(default);

        var result = await new GetClosureWhatsAppStatusHandler(context)
            .Handle(new GetClosureWhatsAppStatusQuery(20), default);

        Assert.Equal(1, result.Unconfirmed);
        Assert.Equal(1, result.InboxUnread);
        Assert.Equal("unconfirmed", Assert.Single(result.Items).DeliveryStatus);
    }

    [Fact]
    public async Task MarkRead_WithClosureToken_MarksOnlyAuthorizedNotification()
    {
        using var context = BazaresContextFactory.Create();
        var customer = new BzaCustomer { Id = 1, TenantId = Tenant, Name = "Cliente", Phone = "5511112222" };
        var total = new BzaClosureCustomerTotal
        {
            Id = 10,
            TenantId = Tenant,
            BzaCustomerId = 1,
            Customer = customer,
            UploadToken = "authorized-token",
        };
        context.ClosureEvents.Add(new BzaClosureEvent
        {
            Id = 20,
            TenantId = Tenant,
            Description = "Cierre",
            PaymentDeadline = DateTime.UtcNow.AddDays(1),
            CustomerTotals = [total],
        });
        context.CustomerInboxNotifications.Add(new BzaCustomerInboxNotification
        {
            Id = 30,
            TenantId = Tenant,
            BzaCustomerId = 1,
            BzaClosureCustomerTotalId = 10,
            Title = "Total",
            Message = "Mensaje",
            ActionUrl = "/comprobante/authorized-token",
        });
        await context.SaveChangesAsync(default);
        var handler = new MarkCustomerNotificationReadHandler(context);

        var result = await handler.Handle(new MarkCustomerNotificationReadCommand(
            "authorized-token", 30, CustomerNotificationAccessKind.ClosureTotal), default);

        Assert.True(result.IsRead);
        Assert.NotNull(context.CustomerInboxNotifications.Single().ReadAt);
    }

    [Fact]
    public async Task MarkRead_WithWrongToken_DoesNotExposeNotification()
    {
        using var context = BazaresContextFactory.Create();
        context.CustomerInboxNotifications.Add(new BzaCustomerInboxNotification
        {
            Id = 30,
            TenantId = Tenant,
            BzaCustomerId = 1,
            BzaClosureCustomerTotalId = 10,
            Title = "Total",
            Message = "Mensaje",
            ActionUrl = "/comprobante/token",
        });
        await context.SaveChangesAsync(default);
        var handler = new MarkCustomerNotificationReadHandler(context);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(
            new MarkCustomerNotificationReadCommand(
                "wrong-token", 30, CustomerNotificationAccessKind.ClosureTotal), default));
    }
}
