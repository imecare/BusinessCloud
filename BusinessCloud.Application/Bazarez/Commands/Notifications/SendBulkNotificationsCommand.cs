using BusinessCloud.Application.Bazares.Common;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.Notifications;

public record SendBulkNotificationsCommand(
    List<int> CustomerTotalIds,
    int NotificationType,
    int ChannelStrategy,
    string? PortalBaseUrl = null) : IRequest<SendBulkNotificationsResultDto>;

public record SendBulkNotificationItemDto(
    int ClosureCustomerTotalId,
    int CustomerId,
    string CustomerName,
    string Channel,
    bool Success,
    string? Error);

public class SendBulkNotificationsResultDto
{
    public int Requested { get; set; }
    public int Processed { get; set; }
    public int PushSent { get; set; }
    public int WhatsAppSent { get; set; }
    public int Failed { get; set; }
    public List<SendBulkNotificationItemDto> Items { get; set; } = new();
}

public class SendBulkNotificationsHandler(
    IBazaresDbContext context,
    IWebPushService webPushService,
    IWhatsAppNotificationService whatsAppNotificationService)
    : IRequestHandler<SendBulkNotificationsCommand, SendBulkNotificationsResultDto>
{
    public async Task<SendBulkNotificationsResultDto> Handle(SendBulkNotificationsCommand request, CancellationToken cancellationToken)
    {
        var ids = request.CustomerTotalIds.Distinct().ToList();
        if (ids.Count == 0)
            throw new InvalidOperationException("No hay clientes seleccionados para notificar.");

        var totals = await context.ClosureCustomerTotals
            .Include(t => t.Customer)
            .Include(t => t.ClosureEvent)
            .Where(t => ids.Contains(t.Id))
            .ToListAsync(cancellationToken);

        var foundIds = totals.Select(t => t.Id).ToHashSet();
        var missing = ids.Where(id => !foundIds.Contains(id)).ToList();
        if (missing.Count > 0)
            throw new KeyNotFoundException("Algunos totales de cliente no existen en el cierre actual.");

        var customerIds = totals.Select(t => t.BzaCustomerId).Distinct().ToList();
        var subscriptions = await context.CustomerNotificationSubscriptions
            .Where(s => customerIds.Contains(s.BzaCustomerId) && s.IsActive)
            .ToListAsync(cancellationToken);

        var subscriptionsByCustomer = subscriptions
            .GroupBy(s => s.BzaCustomerId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new SendBulkNotificationsResultDto { Requested = totals.Count };
        var now = DateTime.UtcNow;

        foreach (var total in totals)
        {
            var channelUsed = string.Empty;
            var success = false;
            string? error = null;

            var template = BuildTemplateData(total, request.NotificationType, request.PortalBaseUrl);
            var hasPush = subscriptionsByCustomer.TryGetValue(total.BzaCustomerId, out var customerSubs) && customerSubs is { Count: > 0 };

            async Task<(bool Ok, string? Err)> TrySendPushAsync()
            {
                if (!hasPush || customerSubs is null)
                    return (false, "Cliente sin suscripcion push activa.");

                if (!webPushService.IsConfigured)
                    return (false, "Web Push no configurado.");

                foreach (var sub in customerSubs)
                {
                    var pushResult = await webPushService.SendAsync(
                        sub.Endpoint,
                        sub.P256dh,
                        sub.Auth,
                        new WebPushMessage(template.Title, template.Body, template.ActionUrl),
                        cancellationToken);

                    if (pushResult.Success)
                    {
                        sub.LastSuccessfulPushAt = now;
                        sub.LastFailureReason = null;
                        sub.LastFailedPushAt = null;
                        return (true, null);
                    }

                    sub.LastFailedPushAt = now;
                    sub.LastFailureReason = pushResult.ErrorMessage;
                }

                return (false, customerSubs.FirstOrDefault()?.LastFailureReason ?? "No se pudo enviar push.");
            }

            async Task<(bool Ok, string? Err)> TrySendWhatsAppAsync()
            {
                var phone = new string((total.Customer?.Phone ?? string.Empty).Where(char.IsDigit).ToArray());
                if (string.IsNullOrWhiteSpace(phone))
                    return (false, "Cliente sin telefono valido.");

                var wa = await whatsAppNotificationService.SendAsync(phone, template, cancellationToken);
                if (wa.Success)
                    return (true, null);

                return (false, wa.ErrorMessage ?? "No se pudo enviar WhatsApp.");
            }

            if (request.ChannelStrategy == NotificationChannelStrategy.OnlyWebPush)
            {
                channelUsed = "WebPush";
                var sent = await TrySendPushAsync();
                success = sent.Ok;
                error = sent.Err;
            }
            else if (request.ChannelStrategy == NotificationChannelStrategy.OnlyWhatsApp)
            {
                channelUsed = "WhatsApp";
                var sent = await TrySendWhatsAppAsync();
                success = sent.Ok;
                error = sent.Err;
            }
            else
            {
                var sentPush = await TrySendPushAsync();
                if (sentPush.Ok)
                {
                    channelUsed = "WebPush";
                    success = true;
                }
                else
                {
                    var sentWa = await TrySendWhatsAppAsync();
                    channelUsed = "WhatsApp";
                    success = sentWa.Ok;
                    error = sentWa.Err ?? sentPush.Err;
                }
            }

            if (channelUsed == "WebPush" && success) result.PushSent++;
            if (channelUsed == "WhatsApp" && success) result.WhatsAppSent++;
            if (!success) result.Failed++;

            result.Processed++;
            result.Items.Add(new SendBulkNotificationItemDto(
                total.Id,
                total.BzaCustomerId,
                total.Customer?.Name ?? "Cliente",
                channelUsed,
                success,
                error));

            context.NotificationLogs.Add(new BzaNotificationLog
            {
                TenantId = total.TenantId,
                BzaClosureEventId = total.BzaClosureEventId,
                BzaClosureCustomerTotalId = total.Id,
                BzaCustomerId = total.BzaCustomerId,
                NotificationType = request.NotificationType,
                Channel = channelUsed == "WebPush" ? NotificationChannel.WebPush : NotificationChannel.WhatsApp,
                Success = success,
                SentAt = now,
                ErrorMessage = error
            });
        }

        await context.SaveChangesAsync(cancellationToken);

        return result;
    }

    private static NotificationTemplateData BuildTemplateData(BzaClosureCustomerTotal total, int notificationType, string? portalBaseUrl)
    {
        var baseUrl = (portalBaseUrl ?? string.Empty).TrimEnd('/');
        var actionUrl = string.IsNullOrWhiteSpace(baseUrl) ? null : $"{baseUrl}/comprobante/{total.UploadToken}";
        var customerName = total.Customer?.Name ?? "Cliente";

        return notificationType switch
        {
            NotificationType.DueToday => new NotificationTemplateData(
                "Tu fecha de pago es hoy",
                $"{customerName}, tu pago vence hoy. Evita retrasos subiendo tu comprobante.",
                actionUrl),

            NotificationType.SaleCancelled => new NotificationTemplateData(
                "Venta cancelada",
                $"{customerName}, tu venta ha sido cancelada. Si tienes dudas, contacta al bazar.",
                actionUrl),

            NotificationType.ProofValidated => new NotificationTemplateData(
                "Comprobante validado",
                $"{customerName}, tu comprobante ya fue aprobado. Gracias por tu pago.",
                actionUrl),

            _ => new NotificationTemplateData(
                "Recordatorio de pago",
                ClosureMessageBuilder.Build(null, customerName, total.TotalAmount, null, total.ClosureEvent.PaymentDeadline, null),
                actionUrl),
        };
    }
}
