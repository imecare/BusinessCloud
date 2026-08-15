using System.Globalization;
using BusinessCloud.Application.Bazares.Common;
using BusinessCloud.Application.Bazares.Queries.IdentifyWhatsAppSender;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Common.Utilities;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
    ISender sender,
    IConfiguration configuration,
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
        {
            changed |= await ApplyStatusAsync(status, cancellationToken);
        }

        if (changed)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        foreach (var message in request.Messages)
        {
            string reply;

            if (string.Equals(message.Type, "text", StringComparison.OrdinalIgnoreCase))
            {
                reply = await BuildReplyAsync(message, cancellationToken);
            }
            else if (IsMediaType(message.Type))
            {
                // El cliente envió una imagen/documento (típicamente su comprobante) a este
                // chat automático. Se le desvía al enlace personalizado para que sí quede
                // registrado en el bazar.
                reply = await BuildMediaDeflectionReplyAsync(message, cancellationToken);
            }
            else
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(reply))
                continue;

            var send = await whatsAppNotificationService.SendAsync(
                message.From,
                new NotificationTemplateData("WhatsApp Bot", reply, null),
                cancellationToken);

            if (!send.Success)
            {
                logger.LogWarning("No se pudo responder por WhatsApp al número {Phone}: {Error}", message.From, send.ErrorMessage);
            }
        }
    }

    private static bool IsMediaType(string? type) =>
        string.Equals(type, "image", StringComparison.OrdinalIgnoreCase)
        || string.Equals(type, "document", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Respuesta cuando el cliente manda su comprobante como imagen/documento a este chat
    /// automático: se le explica que no llega al bazar y se le entrega su enlace personal
    /// (uno por bazar) para subirlo correctamente.
    /// </summary>
    private async Task<string> BuildMediaDeflectionReplyAsync(
        WhatsAppWebhookTextInput message,
        CancellationToken cancellationToken)
    {
        var identified = await sender.Send(new IdentifyWhatsAppSenderQuery(message.From), cancellationToken);

        if (identified.CustomerAccounts.Count == 0)
        {
            return "⚠️ Recibimos tu archivo, pero este es un chat automático y el bazar NO lo recibe.\n\n"
                + "Para que tu pago quede registrado, sube tu comprobante en el enlace que te envió tu bazar. "
                + "Si no lo tienes a la mano, escribe *HOLA* para ver tus opciones.";
        }

        var baseUrl = GetPortalBaseUrl();
        var links = identified.CustomerAccounts
            .GroupBy(x => x.TenantId)
            .Select(g => g.First())
            .OrderBy(x => x.BazarName, StringComparer.CurrentCultureIgnoreCase)
            .Select(x => $"- {x.BazarName}: {baseUrl}/comprobante/{x.UploadToken}");

        return "⚠️ Recibimos tu imagen, pero por este chat automático NO le llega al bazar.\n\n"
            + "Para que tu pago quede registrado, súbela en tu enlace 👇\n"
            + string.Join("\n", links)
            + "\n\nAbre tu enlace y usa el botón para subir el comprobante. "
            + "Para hablar con el bazar, entra al enlace y usa \"Hablar con el bazar\".";
    }

    private async Task<string> BuildReplyAsync(
        WhatsAppWebhookTextInput message,
        CancellationToken cancellationToken)
    {
        var command = NormalizeCommand(message.Body);
        if (command.StartsWith(RecoveryPrefix, StringComparison.OrdinalIgnoreCase))
            return BuildPasswordRecoveryReply(message, command);

        var identified = await sender.Send(new IdentifyWhatsAppSenderQuery(message.From), cancellationToken);
        var intent = ResolveCustomerIntent(message.Body);

        if (intent == CustomerIntent.PendingPayments)
        {
            return identified.CustomerAccounts.Count > 0
                ? BuildCustomerPendingReply(identified)
                : "No encontramos pagos pendientes asociados a este número.";
        }

        if (intent == CustomerIntent.Signatures)
        {
            var signed = await LoadSignedProofsAsync(message.From, cancellationToken);
            return signed.Count > 0
                ? BuildSignaturesReply(signed)
                : "No encontramos comprobantes con firma de entrega en el último mes para este número.";
        }

        if (intent == CustomerIntent.ContactBazar)
        {
            return identified.CustomerAccounts.Count > 0
                ? BuildCustomerBazarContactReply(identified)
                : "No encontramos bazares asociados a este número con pagos pendientes.";
        }

        return identified.Role == WhatsAppSenderRole.Customer
            ? BuildCustomerReply(identified, command)
            : "No encontramos un perfil de cliente asociado a este número. Escribe desde el teléfono registrado en tu bazar.";
    }
    private string BuildPasswordRecoveryReply(WhatsAppWebhookTextInput message, string command)
    {
        var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 3)
            return "Envía el mensaje con el formato: RECUPERAR CONTRASEÑA <ID>";

        var sessionId = parts[2];
        if (!passwordRecoverySessions.TryGetCodeForWhatsApp(sessionId, message.From, out var session))
            return "No encontramos una solicitud activa para este número. Regresa al portal y genera el QR nuevamente.";

        if (string.IsNullOrWhiteSpace(session.VerificationCode))
            return "La solicitud de recuperación aún no está lista. Intenta nuevamente en unos segundos.";

        passwordRecoverySessions.TryMarkCodeDelivered(session.SessionId);
        return $"Código de recuperación para {session.CompanyName}: {session.VerificationCode}. Vence en 5 minutos.";
    }
    private string BuildCustomerReply(IdentifyWhatsAppSenderResultDto identified, string command)
    {
        if (identified.CustomerAccounts.Count == 0)
        {
            return "No encontramos adeudos activos para este número.";
        }

        if (command == "PENDIENTES")
        {
            var lines = identified.CustomerAccounts
                .OrderBy(x => x.BazarName)
                .Select(x => $"- {x.BazarName}: {x.TotalAmount.ToString("C", Culture)}");

            return "Estos son tus bazares con adeudo:\n" + string.Join("\n", lines);
        }

        if (command == "LINKS")
        {
            var baseUrl = (configuration["WhatsApp:PublicPortalBaseUrl"] ?? "https://bazares.bcloud.com.mx").TrimEnd('/');
            var lines = identified.CustomerAccounts
                .OrderBy(x => x.BazarName)
                .Select(x => $"- {x.BazarName}: {baseUrl}/comprobante/{x.UploadToken}");

            return "Estos son tus accesos directos de pago:\n" + string.Join("\n", lines);
        }

        if (command is "HOLA" or "MENU" or "AYUDA" or "INICIO")
            return BuildCustomerMenuReply();

        return BuildCustomerHelpReply(identified);
    }

    private string BuildCustomerHelpReply(IdentifyWhatsAppSenderResultDto identified)
    {
        var proofLinks = identified.CustomerAccounts
            .GroupBy(x => x.TenantId)
            .Select(group => group.First())
            .OrderBy(x => x.BazarName, StringComparer.CurrentCultureIgnoreCase)
            .Select(account => $"- {account.BazarName}: {GetPortalBaseUrl()}/comprobante/{account.UploadToken}");

        return "No reconocimos esa opción. Para hablar con el bazar también puedes abrir tu comprobante y usar el enlace de contacto:\n"
            + string.Join("\n", proofLinks)
            + "\n\n"
            + BuildCustomerMenuReply();
    }
    private static string BuildCustomerMenuReply()
        => "¿Qué deseas consultar?\n"
            + "1. Consultar pendientes\n"
            + "2. Consultar firmas\n"
            + "3. Hablar con un bazar\n\n"
            + "Responde con el número de la opción.";

    private string BuildCustomerBazarContactReply(IdentifyWhatsAppSenderResultDto identified)
    {
        var contacts = identified.CustomerAccounts
            .GroupBy(x => x.TenantId)
            .Select(group => group.First())
            .OrderBy(x => x.BazarName, StringComparer.CurrentCultureIgnoreCase)
            .Select(account =>
            {
                var whatsappLink = ClosureMessageBuilder.BuildWhatsAppLink(account.BazarWhatsApp);
                return whatsappLink is null
                    ? $"- {account.BazarName}: abre tu comprobante para consultar el contacto: {GetPortalBaseUrl()}/comprobante/{account.UploadToken}"
                    : $"- {account.BazarName}: {whatsappLink}";
            });

        return "Selecciona el bazar con el que deseas hablar:\n" + string.Join("\n", contacts);
    }
    private string BuildCustomerPendingReply(IdentifyWhatsAppSenderResultDto identified)
    {
        var baseUrl = GetPortalBaseUrl();
        var lines = identified.CustomerAccounts
            .OrderBy(x => x.BazarName, StringComparer.CurrentCultureIgnoreCase)
            .Select(x => $"- {x.BazarName}: {x.TotalAmount.ToString("C", Culture)}\n  {baseUrl}/comprobante/{x.UploadToken}");

        return "Estos son tus pagos pendientes. Abre el link para subir tu comprobante:\n"
            + string.Join("\n", lines);
    }

    private string BuildSignaturesReply(List<SignedProofDto> proofs)
    {
        var baseUrl = GetPortalBaseUrl();
        var lines = proofs
            .OrderBy(x => x.BazarName, StringComparer.CurrentCultureIgnoreCase)
            .ThenByDescending(x => x.SignedAt)
            .Select(x => $"- {x.BazarName} ({x.SignedAt.ToString("dd/MM/yyyy", Culture)}):\n  {baseUrl}/comprobante/{x.UploadToken}");

        return "Comprobantes con firma de entrega del ultimo mes:\n"
            + string.Join("\n", lines);
    }

    private async Task<List<SignedProofDto>> LoadSignedProofsAsync(string phone, CancellationToken cancellationToken)
    {
        var candidates = PhoneNumberCandidates.Build(phone);
        if (candidates.Count == 0)
            return new List<SignedProofDto>();

        var cutoff = DateTime.UtcNow.AddMonths(-1);

        var rows = await context.ClosureCustomerTotals
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(t => candidates.Contains(t.Customer.Phone)
                        && t.Status != BzaClosureCustomerTotalStatus.Cancelled)
            .Select(t => new
            {
                t.TenantId,
                t.UploadToken,
                LastSignedAt = t.ClosureEvent.DeliveryProofs
                    .Where(p => (p.BzaCollectorGroupId == null || p.BzaCollectorGroupId == t.BzaCollectorGroupId)
                                && p.UploadedAt >= cutoff)
                    .Select(p => (DateTime?)p.UploadedAt)
                    .Max(),
            })
            .Where(x => x.LastSignedAt != null)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
            return new List<SignedProofDto>();

        var tenantIds = rows.Select(x => x.TenantId).Distinct().ToList();
        var bazarNames = await context.BazarSettings
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(s => tenantIds.Contains(s.TenantId))
            .Select(s => new { s.TenantId, s.BazarName })
            .ToDictionaryAsync(x => x.TenantId, x => x.BazarName ?? "Bazar", cancellationToken);

        return rows
            .Select(x => new SignedProofDto(
                bazarNames.TryGetValue(x.TenantId, out var name) ? name : "Bazar",
                x.UploadToken,
                x.LastSignedAt!.Value))
            .ToList();
    }

    private string GetPortalBaseUrl()
        => (configuration["WhatsApp:PublicPortalBaseUrl"] ?? "https://bazares.bcloud.com.mx").TrimEnd('/');

    private static CustomerIntent ResolveCustomerIntent(string? body)
    {
        var text = RemoveDiacritics((body ?? string.Empty).Trim().ToLowerInvariant());
        if (string.IsNullOrWhiteSpace(text))
            return CustomerIntent.None;

        if (text == "2" || text.Contains("firma"))
            return CustomerIntent.Signatures;

        if (text == "1" || text.Contains("pendiente") || text.Contains("pago") || text.Contains("link"))
            return CustomerIntent.PendingPayments;

        if (text == "3" || text.Contains("hablar") || text.Contains("contacto") || text.Contains("bazar"))
            return CustomerIntent.ContactBazar;

        return CustomerIntent.None;
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

    private enum CustomerIntent
    {
        None = 0,
        PendingPayments = 1,
        Signatures = 2,
        ContactBazar = 3,
    }

    private sealed record SignedProofDto(string BazarName, string UploadToken, DateTime SignedAt);

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
}