namespace BusinessCloud.Application.Common.Interfaces;

public record WebPushMessage(string Title, string Body, string? Url);
public record WebPushSendResult(bool Success, string? ErrorMessage = null);

/// <summary>
/// Envio de notificaciones Web Push usando suscripciones de navegador.
/// </summary>
public interface IWebPushService
{
    bool IsConfigured { get; }

    Task<WebPushSendResult> SendAsync(
        string endpoint,
        string p256dh,
        string auth,
        WebPushMessage message,
        CancellationToken cancellationToken = default);
}
