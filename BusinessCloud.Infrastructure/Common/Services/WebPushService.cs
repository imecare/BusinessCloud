using System.Text.Json;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Infrastructure.Common.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebPush;

namespace BusinessCloud.Infrastructure.Common.Services;

public class WebPushService : IWebPushService
{
    private readonly WebPushClient _client;
    private readonly VapidDetails? _vapidDetails;
    private readonly WebPushOptions _options;
    private readonly ILogger<WebPushService> _logger;

    public WebPushService(IOptions<WebPushOptions> options, ILogger<WebPushService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _client = new WebPushClient();
        if (_options.IsConfigured)
        {
            _vapidDetails = new VapidDetails(_options.Subject, _options.PublicKey, _options.PrivateKey);
        }
    }

    public bool IsConfigured => _options.IsConfigured;

    public async Task<WebPushSendResult> SendAsync(
        string endpoint,
        string p256dh,
        string auth,
        WebPushMessage message,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return new WebPushSendResult(false, "Web Push no configurado.");

        if (_vapidDetails is null)
            return new WebPushSendResult(false, "No se pudo inicializar VAPID para Web Push.");

        try
        {
            var subscription = new PushSubscription(endpoint, p256dh, auth);
            var payload = JsonSerializer.Serialize(new
            {
                title = message.Title,
                body = message.Body,
                url = message.Url
            });

            await _client.SendNotificationAsync(subscription, payload, _vapidDetails, cancellationToken: cancellationToken);
            return new WebPushSendResult(true);
        }
        catch (WebPushException ex)
        {
            _logger.LogWarning(ex, "Error WebPush al enviar notificacion.");
            return new WebPushSendResult(false, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepcion inesperada al enviar WebPush.");
            return new WebPushSendResult(false, ex.Message);
        }
    }
}
