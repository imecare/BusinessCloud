using BusinessCloud.Application.Bazarez.Commands.ProcessWhatsAppWebhook;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using BusinessCloud.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BusinessCloud.Tests.Application.Bazares;

public class ProcessWhatsAppWebhookHandlerTests
{
    private const string Tenant = BazaresContextFactory.TenantId;

    [Fact]
    public async Task Handle_StatusFailed_UpdatesExistingMessage()
    {
        using var context = BazaresContextFactory.Create();
        context.WhatsAppMessages.Add(new BzaWhatsAppMessage
        {
            Id = 1,
            TenantId = Tenant,
            WaMessageId = "wamid-1",
            ToPhone = "5215511112222",
            Purpose = "totals",
            Status = "sent",
            SentAt = DateTime.UtcNow,
        });
        await context.SaveChangesAsync(default);

        var handler = CreateHandler(context);
        await handler.Handle(new ProcessWhatsAppWebhookCommand(
            [new("wamid-1", "failed", "5215511112222", 131026, "Undeliverable", "No WhatsApp")],
            []), default);

        var updated = context.WhatsAppMessages.Single(m => m.WaMessageId == "wamid-1");
        Assert.Equal("failed", updated.Status);
        Assert.Equal(131026, updated.ErrorCode);
        Assert.NotNull(updated.StatusUpdatedAt);
    }

    [Fact]
    public async Task Handle_ValidRecoveryMessage_SendsVerificationCodeAndMarksDelivered()
    {
        using var context = BazaresContextFactory.Create();
        var notifications = new Mock<IWhatsAppNotificationService>();
        NotificationTemplateData? sentData = null;
        notifications
            .Setup(n => n.SendAsync("5215511112222", It.IsAny<NotificationTemplateData>(), It.IsAny<CancellationToken>()))
            .Callback<string, NotificationTemplateData, CancellationToken>((_, data, _) => sentData = data)
            .ReturnsAsync(new NotificationSendResult(true));

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
            context,
            notifications.Object,
            sessions.Object,
            NullLogger<ProcessWhatsAppWebhookHandler>.Instance);

        await handler.Handle(new ProcessWhatsAppWebhookCommand(
            [],
            [new("wamid-in-1", "5215511112222", "text", "RECUPERAR CONTRASENA session-1")]), default);

        Assert.NotNull(sentData);
        Assert.Contains("123456", sentData.Body);
        sessions.Verify(s => s.TryMarkCodeDelivered("session-1"), Times.Once);
    }

    [Fact]
    public async Task Handle_UnrelatedText_DoesNotSendReply()
    {
        using var context = BazaresContextFactory.Create();
        var notifications = new Mock<IWhatsAppNotificationService>();
        var handler = new ProcessWhatsAppWebhookHandler(
            context,
            notifications.Object,
            Mock.Of<IPasswordRecoverySessionStore>(),
            NullLogger<ProcessWhatsAppWebhookHandler>.Instance);

        await handler.Handle(new ProcessWhatsAppWebhookCommand(
            [],
            [new("wamid-in-2", "5215511112222", "text", "hola")]), default);

        notifications.Verify(
            n => n.SendAsync(It.IsAny<string>(), It.IsAny<NotificationTemplateData>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static ProcessWhatsAppWebhookHandler CreateHandler(
        BusinessCloud.Infrastructure.Data.BazaresDbContext context)
        => new(
            context,
            Mock.Of<IWhatsAppNotificationService>(),
            Mock.Of<IPasswordRecoverySessionStore>(),
            NullLogger<ProcessWhatsAppWebhookHandler>.Instance);
}