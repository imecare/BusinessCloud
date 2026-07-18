using BusinessCloud.Application.Bazares.Commands.ProcessWhatsAppWebhook;
using BusinessCloud.Application.Bazares.Queries.IdentifyWhatsAppSender;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using BusinessCloud.Tests.TestSupport;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BusinessCloud.Tests.Application.Bazares;

/// <summary>
/// Pruebas del motor conversacional del webhook de WhatsApp: actualización de estatus de
/// mensajes salientes y respuestas automáticas según el rol (Cliente/Dueño).
/// </summary>
public class ProcessWhatsAppWebhookHandlerTests
{
    private const string Tenant = BazaresContextFactory.TenantId;

    private static IConfiguration Config() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["WhatsApp:PublicPortalBaseUrl"] = "https://portal.test",
        })
        .Build();

    private static (Mock<IWhatsAppNotificationService> Notif, List<string> Replies) NotifCapturing()
    {
        var replies = new List<string>();
        var notif = new Mock<IWhatsAppNotificationService>();
        notif
            .Setup(n => n.SendAsync(It.IsAny<string>(), It.IsAny<NotificationTemplateData>(), It.IsAny<CancellationToken>()))
            .Callback<string, NotificationTemplateData, CancellationToken>((_, data, _) => replies.Add(data.Body))
            .ReturnsAsync(new NotificationSendResult(true));
        return (notif, replies);
    }

    [Fact]
    public async Task Handle_StatusFailed_ActualizaMensajeExistente()
    {
        using var ctx = BazaresContextFactory.Create();
        ctx.WhatsAppMessages.Add(new BzaWhatsAppMessage
        {
            Id = 1,
            TenantId = Tenant,
            WaMessageId = "wamid-1",
            ToPhone = "5215511112222",
            Purpose = "totals",
            Status = "sent",
            SentAt = DateTime.UtcNow,
        });
        await ctx.SaveChangesAsync(default);

        var (notif, _) = NotifCapturing();
        var handler = new ProcessWhatsAppWebhookHandler(
            ctx, notif.Object, Mock.Of<ISender>(), Mock.Of<ICacheService>(), Config(),
            NullLogger<ProcessWhatsAppWebhookHandler>.Instance);

        await handler.Handle(new ProcessWhatsAppWebhookCommand(
            new List<WhatsAppWebhookStatusInput>
            {
                new("wamid-1", "failed", "5215511112222", 131026, "Undeliverable", "No WhatsApp"),
            },
            new List<WhatsAppWebhookTextInput>()), default);

        var updated = ctx.WhatsAppMessages.Single(m => m.WaMessageId == "wamid-1");
        Assert.Equal("failed", updated.Status);
        Assert.Equal(131026, updated.ErrorCode);
    }

    [Fact]
    public async Task Handle_ClientePendientes_RespondeListaDeBazares()
    {
        using var ctx = BazaresContextFactory.Create();
        var (notif, replies) = NotifCapturing();

        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(It.IsAny<IdentifyWhatsAppSenderQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentifyWhatsAppSenderResultDto
            {
                NormalizedPhone = "5215511112222",
                Role = WhatsAppSenderRole.Customer,
                CustomerAccounts = new List<CustomerWhatsAppAccountDto>
                {
                    new(1, "tenant-a", "Bazar Uno", 320m, "tok-1", BzaClosureCustomerTotalStatus.Pending),
                },
            });

        var handler = new ProcessWhatsAppWebhookHandler(
            ctx, notif.Object, sender.Object, Mock.Of<ICacheService>(), Config(),
            NullLogger<ProcessWhatsAppWebhookHandler>.Instance);

        await handler.Handle(new ProcessWhatsAppWebhookCommand(
            new List<WhatsAppWebhookStatusInput>(),
            new List<WhatsAppWebhookTextInput>
            {
                new("wamid-in-1", "5215511112222", "text", "pendientes"),
            }), default);

        Assert.Single(replies);
        Assert.Contains("Bazar Uno", replies[0]);
    }

    [Fact]
    public async Task Handle_ClienteLinks_RespondeConEnlacesDelPortal()
    {
        using var ctx = BazaresContextFactory.Create();
        var (notif, replies) = NotifCapturing();

        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(It.IsAny<IdentifyWhatsAppSenderQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentifyWhatsAppSenderResultDto
            {
                NormalizedPhone = "5215511112222",
                Role = WhatsAppSenderRole.Customer,
                CustomerAccounts = new List<CustomerWhatsAppAccountDto>
                {
                    new(1, "tenant-a", "Bazar Uno", 320m, "tok-1", BzaClosureCustomerTotalStatus.Pending),
                },
            });

        var handler = new ProcessWhatsAppWebhookHandler(
            ctx, notif.Object, sender.Object, Mock.Of<ICacheService>(), Config(),
            NullLogger<ProcessWhatsAppWebhookHandler>.Instance);

        await handler.Handle(new ProcessWhatsAppWebhookCommand(
            new List<WhatsAppWebhookStatusInput>(),
            new List<WhatsAppWebhookTextInput>
            {
                new("wamid-in-2", "5215511112222", "text", "LINKS"),
            }), default);

        Assert.Single(replies);
        Assert.Contains("https://portal.test/comprobante/tok-1", replies[0]);
    }

    [Fact]
    public async Task Handle_ClienteSaludo_RespondeMenu()
    {
        using var ctx = BazaresContextFactory.Create();
        var (notif, replies) = NotifCapturing();

        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(It.IsAny<IdentifyWhatsAppSenderQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentifyWhatsAppSenderResultDto
            {
                NormalizedPhone = "5215511112222",
                Role = WhatsAppSenderRole.Customer,
                CustomerAccounts = new List<CustomerWhatsAppAccountDto>
                {
                    new(1, "tenant-a", "Bazar Uno", 320m, "tok-1", BzaClosureCustomerTotalStatus.Pending),
                },
            });

        var handler = new ProcessWhatsAppWebhookHandler(
            ctx, notif.Object, sender.Object, Mock.Of<ICacheService>(), Config(),
            NullLogger<ProcessWhatsAppWebhookHandler>.Instance);

        await handler.Handle(new ProcessWhatsAppWebhookCommand(
            new List<WhatsAppWebhookStatusInput>(),
            new List<WhatsAppWebhookTextInput>
            {
                new("wamid-in-3", "5215511112222", "text", "hola"),
            }), default);

        Assert.Single(replies);
        Assert.Contains("PENDIENTES", replies[0]);
        Assert.Contains("LINKS", replies[0]);
    }

    [Fact]
    public async Task Handle_DuenoUnCierreAbierto_RespondeResumen()
    {
        using var ctx = BazaresContextFactory.Create();
        ctx.BazarSettings.Add(new BzaBazarSettings { Id = 1, TenantId = Tenant, BazarName = "Bazar Dueño" });
        ctx.ClosureEvents.Add(new BzaClosureEvent
        {
            Id = 10,
            TenantId = Tenant,
            Description = "Cierre semanal",
            PaymentDeadline = DateTime.UtcNow.AddDays(2),
            Status = BzaClosureEventStatus.PendingPayment,
            CustomerTotals = new List<BzaClosureCustomerTotal>
            {
                new() { Id = 1, TenantId = Tenant, BzaClosureEventId = 10, BzaCustomerId = 1, UploadToken = "t1", Status = BzaClosureCustomerTotalStatus.ProofReceived },
                new() { Id = 2, TenantId = Tenant, BzaClosureEventId = 10, BzaCustomerId = 2, UploadToken = "t2", Status = BzaClosureCustomerTotalStatus.Pending },
            },
        });
        await ctx.SaveChangesAsync(default);

        var (notif, replies) = NotifCapturing();
        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(It.IsAny<IdentifyWhatsAppSenderQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentifyWhatsAppSenderResultDto
            {
                NormalizedPhone = "5215500000000",
                Role = WhatsAppSenderRole.Owner,
                OwnerTenants = new List<OwnerWhatsAppTenantDto>
                {
                    new(Tenant, "Empresa Dueño", "Bazar Dueño"),
                },
            });

        var cache = new Mock<ICacheService>();
        cache.Setup(c => c.GetAsync<int?>(It.IsAny<string>())).ReturnsAsync((int?)null);

        var handler = new ProcessWhatsAppWebhookHandler(
            ctx, notif.Object, sender.Object, cache.Object, Config(),
            NullLogger<ProcessWhatsAppWebhookHandler>.Instance);

        await handler.Handle(new ProcessWhatsAppWebhookCommand(
            new List<WhatsAppWebhookStatusInput>(),
            new List<WhatsAppWebhookTextInput>
            {
                new("wamid-in-4", "5215500000000", "text", "hola"),
            }), default);

        Assert.Single(replies);
        Assert.Contains("Clientes con comprobante: 1", replies[0]);
        Assert.Contains("Clientes pendientes por pagar: 1", replies[0]);
    }
}
