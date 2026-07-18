namespace BusinessCloud.Application.Common.Interfaces;

public record NotificationTemplateData(string Title, string Body, string? ActionUrl);
public record NotificationSendResult(bool Success, string? ErrorMessage = null, string? MessageId = null);

/// <summary>
/// Envio de notificaciones por WhatsApp para eventos de cierre.
/// </summary>
public interface IWhatsAppNotificationService
{
    Task<NotificationSendResult> SendAsync(string phone, NotificationTemplateData data, CancellationToken cancellationToken = default);
}
