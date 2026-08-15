using BusinessCloud.Application.Bazarez.Commands.ProcessWhatsAppWebhook;
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
            ctx, notif.Object, Mock.Of<ISender>(), Config(), Mock.Of<IPasswordRecoverySessionStore>(),
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
                    new(1, "tenant-a", "Bazar Uno", 320m, "tok-1", BzaClosureCustomerTotalStatus.Pending, null),
                },
            });

        var handler = new ProcessWhatsAppWebhookHandler(
            ctx, notif.Object, sender.Object, Config(), Mock.Of<IPasswordRecoverySessionStore>(),
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
                    new(1, "tenant-a", "Bazar Uno", 320m, "tok-1", BzaClosureCustomerTotalStatus.Pending, null),
                },
            });

        var handler = new ProcessWhatsAppWebhookHandler(
            ctx, notif.Object, sender.Object, Config(), Mock.Of<IPasswordRecoverySessionStore>(),
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
                    new(1, "tenant-a", "Bazar Uno", 320m, "tok-1", BzaClosureCustomerTotalStatus.Pending, null),
                },
            });

        var handler = new ProcessWhatsAppWebhookHandler(
            ctx, notif.Object, sender.Object, Config(), Mock.Of<IPasswordRecoverySessionStore>(),
            NullLogger<ProcessWhatsAppWebhookHandler>.Instance);

        await handler.Handle(new ProcessWhatsAppWebhookCommand(
            new List<WhatsAppWebhookStatusInput>(),
            new List<WhatsAppWebhookTextInput>
            {
                new("wamid-in-3", "5215511112222", "text", "hola"),
            }), default);

        Assert.Single(replies);
        Assert.Contains("1. Consultar pendientes", replies[0]);
        Assert.Contains("2. Consultar firmas", replies[0]);
        Assert.Contains("3. Hablar con un bazar", replies[0]);
    }

    [Fact]
    public async Task Handle_ClienteFirmas_RespondeComprobantesConFirmaDelUltimoMes()
    {
        using var ctx = BazaresContextFactory.Create();
        ctx.BazarSettings.Add(new BzaBazarSettings { Id = 1, TenantId = Tenant, BazarName = "Bazar Firmas" });

        var customer = new BzaCustomer { Id = 1, TenantId = Tenant, Name = "Cliente Uno", Phone = "5511112222" };
        ctx.Customers.Add(customer);

        ctx.ClosureEvents.Add(new BzaClosureEvent
        {
            Id = 30,
            TenantId = Tenant,
            Description = "Cierre con entrega",
            PaymentDeadline = DateTime.UtcNow.AddDays(-1),
            Status = BzaClosureEventStatus.Validated,
            DeliveryProofs = new List<BzaClosureDeliveryProof>
            {
                new() { Id = 1, TenantId = Tenant, BzaClosureEventId = 30, BzaCollectorGroupId = null, ImageUrl = "firma.jpg", UploadedAt = DateTime.UtcNow.AddDays(-3) },
            },
            CustomerTotals = new List<BzaClosureCustomerTotal>
            {
                new() { Id = 1, TenantId = Tenant, BzaClosureEventId = 30, BzaCustomerId = 1, Customer = customer, UploadToken = "tok-firma", Status = BzaClosureCustomerTotalStatus.Validated },
            },
        });
        await ctx.SaveChangesAsync(default);

        var (notif, replies) = NotifCapturing();
        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(It.IsAny<IdentifyWhatsAppSenderQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentifyWhatsAppSenderResultDto
            {
                NormalizedPhone = "5511112222",
                Role = WhatsAppSenderRole.Unknown,
            });

        var handler = new ProcessWhatsAppWebhookHandler(
            ctx, notif.Object, sender.Object, Config(), Mock.Of<IPasswordRecoverySessionStore>(),
            NullLogger<ProcessWhatsAppWebhookHandler>.Instance);

        await handler.Handle(new ProcessWhatsAppWebhookCommand(
            new List<WhatsAppWebhookStatusInput>(),
            new List<WhatsAppWebhookTextInput>
            {
                new("wamid-in-4", "5511112222", "text", "firmas"),
            }), default);

        Assert.Single(replies);
        Assert.Contains("Bazar Firmas", replies[0]);
        Assert.Contains("https://portal.test/comprobante/tok-firma", replies[0]);
    }

    [Fact]
    public async Task Handle_RemitenteQueNoEsCliente_RespondeSinConsultarComoDueno()
    {
        using var ctx = BazaresContextFactory.Create();
        var (notif, replies) = NotifCapturing();
        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(It.IsAny<IdentifyWhatsAppSenderQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentifyWhatsAppSenderResultDto
            {
                NormalizedPhone = "5215500000000",
                Role = WhatsAppSenderRole.Unknown,
            });

        var handler = new ProcessWhatsAppWebhookHandler(
            ctx, notif.Object, sender.Object, Config(), Mock.Of<IPasswordRecoverySessionStore>(),
            NullLogger<ProcessWhatsAppWebhookHandler>.Instance);

        await handler.Handle(new ProcessWhatsAppWebhookCommand(
            [],
            [new("wamid-unknown-sender", "5215500000000", "text", "hola")]), default);

        var reply = Assert.Single(replies);
        Assert.Contains("perfil de cliente", reply);
        Assert.DoesNotContain("cierre", reply, StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public async Task Handle_ClienteOpcionTres_RespondeWhatsAppDeSusBazares()
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
                CustomerAccounts =
                [
                    new(1, "tenant-a", "Bazar Uno", 320m, "tok-actual", BzaClosureCustomerTotalStatus.Pending, "55 1234 5678"),
                ],
            });

        var handler = new ProcessWhatsAppWebhookHandler(
            ctx, notif.Object, sender.Object, Config(),
            Mock.Of<IPasswordRecoverySessionStore>(),
            NullLogger<ProcessWhatsAppWebhookHandler>.Instance);

        await handler.Handle(new ProcessWhatsAppWebhookCommand(
            [],
            [new("wamid-contact", "5215511112222", "text", "3")]), default);

        var reply = Assert.Single(replies);
        Assert.Contains("Bazar Uno", reply);
        Assert.Contains("https://wa.me/525512345678", reply);
    }
    [Fact]
    public async Task Handle_ClienteMensajeDesconocido_RespondeGuiaYMenu()
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
                CustomerAccounts =
                [
                    new(1, "tenant-a", "Bazar Uno", 320m, "tok-actual", BzaClosureCustomerTotalStatus.Pending, null),
                ],
            });

        var handler = new ProcessWhatsAppWebhookHandler(
            ctx, notif.Object, sender.Object, Config(),
            Mock.Of<IPasswordRecoverySessionStore>(),
            NullLogger<ProcessWhatsAppWebhookHandler>.Instance);

        await handler.Handle(new ProcessWhatsAppWebhookCommand(
            [],
            [new("wamid-unknown", "5215511112222", "text", "mensaje diferente")]), default);

        var reply = Assert.Single(replies);
        Assert.Contains("https://portal.test/comprobante/tok-actual", reply);
        Assert.Contains("3. Hablar con un bazar", reply);
    }
    [Fact]
    public async Task Handle_RecuperacionValida_EnviaCodigoYMarcaEntrega()
    {
        using var ctx = BazaresContextFactory.Create();
        var (notif, replies) = NotifCapturing();
        var recoverySession = new PasswordRecoverySession(
            "session-1",
            Tenant,
            "owner@example.com",
            "Bazar Uno",
            "5215511112222",
            PasswordRecoveryChannel.WhatsApp,
            "*******2222",
            DateTime.UtcNow.AddMinutes(5))
        {
            VerificationCode = "123456",
        };
        var sessions = new Mock<IPasswordRecoverySessionStore>();
        sessions
            .Setup(s => s.TryGetCodeForWhatsApp("SESSION-1", "5215511112222", out recoverySession))
            .Returns(true);
        sessions.Setup(s => s.TryMarkCodeDelivered("session-1")).Returns(true);

        var handler = new ProcessWhatsAppWebhookHandler(
            ctx, notif.Object, Mock.Of<ISender>(), Config(),
            sessions.Object,
            NullLogger<ProcessWhatsAppWebhookHandler>.Instance);

        await handler.Handle(new ProcessWhatsAppWebhookCommand(
            [],
            [new("wamid-recovery", "5215511112222", "text", "RECUPERAR CONTRASEÑA session-1")]), default);

        var reply = Assert.Single(replies);
        Assert.Contains("123456", reply);
        sessions.Verify(s => s.TryMarkCodeDelivered("session-1"), Times.Once);
    }

    [Fact]
    public async Task Handle_ImagenComprobante_ClienteIdentificado_RespondeConEnlacePersonalizado()
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
                    new(1, "tenant-a", "Bazar Uno", 320m, "tok-1", BzaClosureCustomerTotalStatus.Pending, null),
                },
            });

        var handler = new ProcessWhatsAppWebhookHandler(
            ctx, notif.Object, sender.Object, Config(), Mock.Of<IPasswordRecoverySessionStore>(),
            NullLogger<ProcessWhatsAppWebhookHandler>.Instance);

        await handler.Handle(new ProcessWhatsAppWebhookCommand(
            new List<WhatsAppWebhookStatusInput>(),
            new List<WhatsAppWebhookTextInput>
            {
                new("wamid-img-1", "5215511112222", "image", string.Empty),
            }), default);

        var reply = Assert.Single(replies);
        Assert.Contains("NO LE LLEGÓ AL BAZAR", reply);
        Assert.Contains("Bazar Uno", reply);
        Assert.Contains("link para subir comprobante", reply);
        Assert.Contains("https://portal.test/comprobante/tok-1", reply);
    }

    [Fact]
    public async Task Handle_DocumentoComprobante_ClienteIdentificado_RespondeConEnlace()
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
                    new(1, "tenant-a", "Bazar Uno", 320m, "tok-1", BzaClosureCustomerTotalStatus.Pending, null),
                },
            });

        var handler = new ProcessWhatsAppWebhookHandler(
            ctx, notif.Object, sender.Object, Config(), Mock.Of<IPasswordRecoverySessionStore>(),
            NullLogger<ProcessWhatsAppWebhookHandler>.Instance);

        await handler.Handle(new ProcessWhatsAppWebhookCommand(
            new List<WhatsAppWebhookStatusInput>(),
            new List<WhatsAppWebhookTextInput>
            {
                new("wamid-doc-1", "5215511112222", "document", string.Empty),
            }), default);

        var reply = Assert.Single(replies);
        Assert.Contains("https://portal.test/comprobante/tok-1", reply);
    }

    [Fact]
    public async Task Handle_ImagenComprobante_ClienteNoIdentificado_RespondeGenerico()
    {
        using var ctx = BazaresContextFactory.Create();
        var (notif, replies) = NotifCapturing();

        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(It.IsAny<IdentifyWhatsAppSenderQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentifyWhatsAppSenderResultDto
            {
                NormalizedPhone = "5215599998888",
                Role = WhatsAppSenderRole.Unknown,
                CustomerAccounts = new List<CustomerWhatsAppAccountDto>(),
            });

        var handler = new ProcessWhatsAppWebhookHandler(
            ctx, notif.Object, sender.Object, Config(), Mock.Of<IPasswordRecoverySessionStore>(),
            NullLogger<ProcessWhatsAppWebhookHandler>.Instance);

        await handler.Handle(new ProcessWhatsAppWebhookCommand(
            new List<WhatsAppWebhookStatusInput>(),
            new List<WhatsAppWebhookTextInput>
            {
                new("wamid-img-2", "5215599998888", "image", string.Empty),
            }), default);

        var reply = Assert.Single(replies);
        Assert.Contains("chat automático", reply);
        Assert.DoesNotContain("/comprobante/", reply);
    }
}
