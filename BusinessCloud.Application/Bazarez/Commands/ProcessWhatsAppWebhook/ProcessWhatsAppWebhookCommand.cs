using System.Globalization;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BusinessCloud.Application.Bazarez.Commands.ProcessWhatsAppWebhook;

public record WhatsAppWebhookStatusInput(
    string MessageId,
    string Status,
    string? RecipientId,
    int? ErrorCode,
    string? ErrorTitle,
    string? ErrorMessage);

public record WhatsAppWebhookTextInput(
    string MessageId,
    string From,
    string Type,
    string Body);

public record ProcessWhatsAppWebhookCommand(
    List<WhatsAppWebhookStatusInput> Statuses,
    List<WhatsAppWebhookTextInput> Messages) : IRequest;

public class ProcessWhatsAppWebhookHandler(
    IBazaresDbContext context,
    IWhatsAppNotificationService whatsAppNotificationService,
    IPasswordRecoverySessionStore passwordRecoverySessions,
    ILogger<ProcessWhatsAppWebhookHandler> logger)
    : IRequestHandler<ProcessWhatsAppWebhookCommand>
{
    private static readonly CultureInfo Culture = new("es-MX");
    private const string RecoveryPrefix = "RECUPERAR CONTRASENA";

    public async Task Handle(ProcessWhatsAppWebhookCommand request, CancellationToken cancellationToken)
    {
        var changed = false;

        foreach (var status in request.Statuses)
            changed |= await ApplyStatusAsync(status, cancellationToken);

        if (changed)
            await context.SaveChangesAsync(cancellationToken);

        foreach (var message in request.Messages)
        {
            if (!string.Equals(message.Type, "text", StringComparison.OrdinalIgnoreCase))
                continue;

            var reply = BuildReply(message);
            if (string.IsNullOrWhiteSpace(reply))
                continue;

            var send = await whatsAppNotificationService.SendAsync(
                message.From,
                new NotificationTemplateData("WhatsApp Bot", reply, null),
                cancellationToken);

            if (!send.Success)
                logger.LogWarning("No se pudo responder por WhatsApp al numero {Phone}: {Error}", message.From, send.ErrorMessage);
        }
    }

    private string? BuildReply(WhatsAppWebhookTextInput message)
    {
        var normalized = NormalizeCommand(message.Body);
        if (!normalized.StartsWith(RecoveryPrefix, StringComparison.OrdinalIgnoreCase))
            return null;

        var parts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 3)
            return "Envia el mensaje con el formato: RECUPERAR CONTRASENA <ID>";

        var sessionId = parts[2];
        if (!passwordRecoverySessions.TryGetCodeForWhatsApp(sessionId, message.From, out var session))
            return "No encontramos una solicitud activa para este numero. Regresa al portal y genera el QR nuevamente.";

        if (string.IsNullOrWhiteSpace(session.VerificationCode))
            return "La solicitud de recuperacion aun no esta lista. Intenta nuevamente en unos segundos.";

        passwordRecoverySessions.TryMarkCodeDelivered(session.SessionId);
        return $"Codigo de recuperacion para {session.CompanyName}: {session.VerificationCode}. Vence en 5 minutos.";
    }

    private async Task<bool> ApplyStatusAsync(WhatsAppWebhookStatusInput status, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(status.MessageId) || string.IsNullOrWhiteSpace(status.Status))
            return false;

        var now = DateTime.UtcNow;
        var existing = await context.WhatsAppMessages
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.WaMessageId == status.MessageId, cancellationToken);

        if (existing is null)
        {
            context.WhatsAppMessages.Add(new BzaWhatsAppMessage
            {
                WaMessageId = status.MessageId,
                ToPhone = status.RecipientId ?? string.Empty,
                Purpose = "unknown",
                Status = status.Status,
                ErrorCode = status.ErrorCode,
                ErrorTitle = status.ErrorTitle,
                ErrorMessage = status.ErrorMessage,
                SentAt = now,
                StatusUpdatedAt = now,
            });
            return true;
        }

        existing.Status = status.Status;
        existing.StatusUpdatedAt = now;
        if (status.Status == "failed")
        {
            existing.ErrorCode = status.ErrorCode;
            existing.ErrorTitle = status.ErrorTitle;
            existing.ErrorMessage = status.ErrorMessage;
        }

        return true;
    }

    private static string NormalizeCommand(string? value)
    {
        var text = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return RemoveDiacritics(text).ToUpperInvariant();
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
        var builder = new System.Text.StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                builder.Append(ch);
        }

        return builder.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }
}
