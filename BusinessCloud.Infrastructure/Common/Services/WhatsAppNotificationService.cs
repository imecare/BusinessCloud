using BusinessCloud.Application.Common.Interfaces;

namespace BusinessCloud.Infrastructure.Common.Services;

public class WhatsAppNotificationService(IWhatsAppSender sender) : IWhatsAppNotificationService
{
    public async Task<NotificationSendResult> SendAsync(string phone, NotificationTemplateData data, CancellationToken cancellationToken = default)
    {
        var body = data.Body;
        if (!string.IsNullOrWhiteSpace(data.ActionUrl))
        {
            body = $"{body}\n\n{data.ActionUrl}";
        }

        var result = await sender.SendTextWithResultAsync(phone, body, cancellationToken);
        return new NotificationSendResult(result.Success, result.ErrorMessage, result.MessageId);
    }
}
