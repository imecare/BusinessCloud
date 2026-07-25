using BusinessCloud.Application.Bazares.Common;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
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
    IWhatsAppNotificationService whatsAppNotificationService,
    IHttpContextAccessor httpContextAccessor)
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

        var tenantIds = totals.Select(t => t.TenantId).Distinct().ToList();
        var bazarSettingsByTenant = await context.BazarSettings
            .IgnoreQueryFilters()
            .Where(s => tenantIds.Contains(s.TenantId))
            .ToDictionaryAsync(s => s.TenantId, cancellationToken);

        var notificationSettingsByTenant = await context.NotificationSettings
            .IgnoreQueryFilters()
            .Where(s => tenantIds.Contains(s.TenantId))
            .ToDictionaryAsync(s => s.TenantId, cancellationToken);

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

            var template = BuildTemplateData(total, request.NotificationType, request.PortalBaseUrl, notificationSettingsByTenant.GetValueOrDefault(total.TenantId));
            var iconUrl = BuildIconUrl(bazarSettingsByTenant.GetValueOrDefault(total.TenantId));
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
                        new WebPushMessage(template.Title, template.Body, template.ActionUrl, iconUrl),
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

    private string? BuildIconUrl(BzaBazarSettings? settings)
    {
        var logoUrl = settings?.LogoUrl;
        if (string.IsNullOrWhiteSpace(logoUrl))
            return null;

        if (logoUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return logoUrl;

        var request = httpContextAccessor.HttpContext?.Request;
        if (request is null)
            return logoUrl;

        var baseUrl = $"{request.Scheme}://{request.Host}";
        return logoUrl.StartsWith('/') ? $"{baseUrl}{logoUrl}" : $"{baseUrl}/{logoUrl}";
    }

    private static NotificationTemplateData BuildTemplateData(
        BzaClosureCustomerTotal total,
        int notificationType,
        string? portalBaseUrl,
        BzaNotificationSettings? settings)
    {
        var baseUrl = (portalBaseUrl ?? string.Empty).TrimEnd('/');
        var actionUrl = string.IsNullOrWhiteSpace(baseUrl) ? null : $"{baseUrl}/comprobante/{total.UploadToken}";
        var customerName = total.Customer?.Name ?? "Cliente";

        return notificationType switch
        {
            NotificationType.DueToday => BuildDueDateTemplate(customerName, total, actionUrl, settings),

            NotificationType.SaleCancelled => new NotificationTemplateData(
                "Venta cancelada",
                !string.IsNullOrWhiteSpace(settings?.SaleCancelledMessage)
                    ? settings!.SaleCancelledMessage
                    : $"{customerName}, tu venta ha sido cancelada. Si tienes dudas, contacta al bazar.",
                actionUrl),

            NotificationType.ProofValidated => new NotificationTemplateData(
                "Comprobante validado",
                !string.IsNullOrWhiteSpace(settings?.ProofValidatedMessage)
                    ? settings!.ProofValidatedMessage
                    : $"{customerName}, tu comprobante ya fue aprobado. Gracias por tu pago.",
                actionUrl),

            _ => BuildReminderTemplate(customerName, total, actionUrl),
        };
    }

    /// <summary>
    /// Arma el recordatorio de pago (tipo por defecto). Si el total esta actualmente rechazado,
    /// se reemplaza por un mensaje especifico con el motivo del rechazo y la invitacion a
    /// volver a subir el comprobante, acompanado del link. En cualquier otro caso, se conserva
    /// el comportamiento existente (mensaje de cobro con el enlace embebido).
    /// </summary>
    private static NotificationTemplateData BuildReminderTemplate(string customerName, BzaClosureCustomerTotal total, string? actionUrl)
    {
        if (total.Status == BzaClosureCustomerTotalStatus.Rejected)
        {
            var reason = string.IsNullOrWhiteSpace(total.RejectionReason)
                ? "no cumple con lo requerido"
                : total.RejectionReason;

            var rejectedBody = $"{customerName}, tu comprobante fue rechazado. Motivo: {reason}. " +
                "Puedes volver a subir tu comprobante aqui:";
            rejectedBody = AppendLink(rejectedBody, actionUrl);

            return new NotificationTemplateData("Comprobante rechazado", rejectedBody, actionUrl);
        }

        var body = ClosureMessageBuilder.Build(null, customerName, total.TotalAmount, null, total.ClosureEvent.PaymentDeadline, null);
        body = string.IsNullOrWhiteSpace(actionUrl)
            ? body.Replace(ClosureMessageBuilder.UploadLinkPlaceholder, string.Empty)
            : body.Replace(ClosureMessageBuilder.UploadLinkPlaceholder, actionUrl);

        return new NotificationTemplateData("Recordatorio de pago", body, actionUrl);
    }

    /// <summary>
    /// Arma el mensaje de vencimiento usando los textos configurados por el bazar:
    /// "por vencer" si la fecha limite de pago aun no llega, o "vencido" si ya se cumplio ese
    /// dia o antes. Ambos casos incluyen el link del comprobante en el cuerpo del mensaje.
    /// </summary>
    private static NotificationTemplateData BuildDueDateTemplate(
        string customerName,
        BzaClosureCustomerTotal total,
        string? actionUrl,
        BzaNotificationSettings? settings)
    {
        var isOverdue = total.ClosureEvent.PaymentDeadline.Date <= DateTime.UtcNow.Date;

        if (isOverdue)
        {
            var body = !string.IsNullOrWhiteSpace(settings?.PaymentOverdueMessage)
                ? settings!.PaymentOverdueMessage
                : $"{customerName}, tu pago se encuentra vencido, por favor regularizalo.";

            return new NotificationTemplateData("Pago vencido", AppendLink(body, actionUrl), actionUrl);
        }

        var dueSoonBody = !string.IsNullOrWhiteSpace(settings?.PaymentDueSoonMessage)
            ? settings!.PaymentDueSoonMessage
            : $"{customerName}, tu pago vence hoy. Evita retrasos subiendo tu comprobante.";

        return new NotificationTemplateData("Tu pago esta por vencer", AppendLink(dueSoonBody, actionUrl), actionUrl);
    }

    /// <summary>Agrega el link del comprobante al cuerpo del mensaje si aun no esta presente.</summary>
    private static string AppendLink(string body, string? actionUrl)
    {
        if (string.IsNullOrWhiteSpace(actionUrl) || body.Contains(actionUrl, StringComparison.Ordinal))
            return body;

        return $"{body}\n\n{actionUrl}";
    }
}
