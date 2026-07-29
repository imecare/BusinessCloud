using System.Globalization;
using BusinessCloud.Application.Bazares.Queries.IdentifyWhatsAppSender;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BusinessCloud.Application.Bazares.Commands.ProcessWhatsAppWebhook;

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

internal sealed record OwnerClosureSummaryDto(
    int ClosureEventId,
    string TenantId,
    string BazarName,
    string Description,
    DateTime PaymentDeadline,
    int ProofReceivedCount,
    int PendingCount);

public class ProcessWhatsAppWebhookHandler(
    IBazaresDbContext context,
    IWhatsAppNotificationService whatsAppNotificationService,
    ISender sender,
    ICacheService cache,
    IConfiguration configuration,
    ILogger<ProcessWhatsAppWebhookHandler> logger)
    : IRequestHandler<ProcessWhatsAppWebhookCommand>
{
    private static readonly CultureInfo Culture = new("es-MX");
    private const string OwnerSelectionPrefix = "wa-owner-selected-closure:";

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
            if (!string.Equals(message.Type, "text", StringComparison.OrdinalIgnoreCase))
                continue;

            var reply = await BuildReplyAsync(message, cancellationToken);
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

    private async Task<string> BuildReplyAsync(WhatsAppWebhookTextInput message, CancellationToken cancellationToken)
    {
        var identified = await sender.Send(new IdentifyWhatsAppSenderQuery(message.From), cancellationToken);
        var command = NormalizeCommand(message.Body);
        var intent = ResolveCustomerIntent(message.Body);

        // Palabras clave de cliente: si el numero pertenece a un cliente se atiende como
        // cliente aunque tambien sea dueno (un dueno puede ser cliente de otros bazares).
        if (intent == CustomerIntent.PendingPayments)
        {
            if (identified.CustomerAccounts.Count > 0)
                return BuildCustomerPendingReply(identified);

            if (identified.Role == WhatsAppSenderRole.Owner)
                return await BuildOwnerReplyAsync(identified, command, cancellationToken);

            return "No encontramos pagos pendientes asociados a este numero.";
        }

        if (intent == CustomerIntent.Signatures)
        {
            var signed = await LoadSignedProofsAsync(message.From, cancellationToken);
            if (signed.Count > 0)
                return BuildSignaturesReply(signed);

            if (identified.Role == WhatsAppSenderRole.Owner && identified.CustomerAccounts.Count == 0)
                return await BuildOwnerReplyAsync(identified, command, cancellationToken);

            return "No encontramos comprobantes con firma de entrega en el ultimo mes para este numero.";
        }

        return identified.Role switch
        {
            WhatsAppSenderRole.Owner => await BuildOwnerReplyAsync(identified, command, cancellationToken),
            WhatsAppSenderRole.Customer => BuildCustomerReply(identified, command),
            _ => "No encontramos un perfil asociado a este número. Si eres cliente, escribe desde el teléfono registrado en tu bazar."
        };
    }

    private async Task<string> BuildOwnerReplyAsync(IdentifyWhatsAppSenderResultDto identified, string command, CancellationToken cancellationToken)
    {
        var tenantIds = identified.OwnerTenants.Select(x => x.TenantId).Distinct().ToList();
        var openClosures = await LoadOwnerOpenClosuresAsync(tenantIds, cancellationToken);

        if (openClosures.Count == 0)
        {
            return "No encontramos cierres abiertos para tu bazar en este momento.";
        }

        var cacheKey = OwnerSelectionPrefix + identified.NormalizedPhone;
        var selectedClosureId = await cache.GetAsync<int?>(cacheKey);

        if (selectedClosureId.HasValue && openClosures.All(x => x.ClosureEventId != selectedClosureId.Value))
        {
            await cache.RemoveAsync(cacheKey);
            selectedClosureId = null;
        }

        if (int.TryParse(command, out var requestedClosureId))
        {
            var selected = openClosures.FirstOrDefault(x => x.ClosureEventId == requestedClosureId);
            if (selected is null)
            {
                return BuildOwnerSelectionPrompt(openClosures, "No encontré ese cierre entre tus cierres abiertos.");
            }

            await cache.SetAsync<int?>(cacheKey, selected.ClosureEventId, TimeSpan.FromHours(12));
            return BuildOwnerSummary(selected, true);
        }

        if (openClosures.Count == 1)
        {
            var selected = openClosures[0];
            await cache.SetAsync<int?>(cacheKey, selected.ClosureEventId, TimeSpan.FromHours(12));
            return BuildOwnerSummary(selected, false);
        }

        if (selectedClosureId.HasValue)
        {
            var selected = openClosures.FirstOrDefault(x => x.ClosureEventId == selectedClosureId.Value);
            if (selected is not null)
            {
                return BuildOwnerSummary(selected, false);
            }
        }

        return BuildOwnerSelectionPrompt(openClosures, null);
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

        return "Hola. Escribe PAGOS para ver tus pagos pendientes con el link a tu comprobante, o FIRMAS para ver tus comprobantes de entrega del ultimo mes.";
    }

    private async Task<List<OwnerClosureSummaryDto>> LoadOwnerOpenClosuresAsync(List<string> tenantIds, CancellationToken cancellationToken)
    {
        var bazarNames = await context.BazarSettings
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(s => tenantIds.Contains(s.TenantId))
            .Select(s => new { s.TenantId, s.BazarName })
            .ToDictionaryAsync(x => x.TenantId, x => x.BazarName ?? "Bazar", cancellationToken);

        return await context.ClosureEvents
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(c => tenantIds.Contains(c.TenantId)
                        && c.Status != BzaClosureEventStatus.Validated
                        && c.Status != BzaClosureEventStatus.Cancelled)
            .Select(c => new OwnerClosureSummaryDto(
                c.Id,
                c.TenantId,
                string.Empty,
                c.Description,
                c.PaymentDeadline,
                c.CustomerTotals.Count(t => t.Status == BzaClosureCustomerTotalStatus.ProofReceived),
                c.CustomerTotals.Count(t => t.Status == BzaClosureCustomerTotalStatus.Pending || t.Status == BzaClosureCustomerTotalStatus.Rejected)))
            .ToListAsync(cancellationToken)
            .ContinueWith(task => task.Result
                .Select(c => c with { BazarName = bazarNames.TryGetValue(c.TenantId, out var bazarName) ? bazarName : "Bazar" })
                .OrderBy(c => c.BazarName)
                .ThenBy(c => c.ClosureEventId)
                .ToList(), cancellationToken);
    }

    private static string BuildOwnerSelectionPrompt(List<OwnerClosureSummaryDto> openClosures, string? prefix)
    {
        var lines = openClosures.Select(c =>
            $"- [{c.ClosureEventId}] {c.BazarName}: {c.Description} | comprobantes: {c.ProofReceivedCount} | pendientes: {c.PendingCount}");

        var header = string.IsNullOrWhiteSpace(prefix)
            ? "Tienes varios cierres abiertos. Responde con el número de cierre para ver detalles:"
            : prefix + "\n\nResponde con el número de cierre para ver detalles:";

        return header + "\n" + string.Join("\n", lines);
    }

    private static string BuildOwnerSummary(OwnerClosureSummaryDto closure, bool selectionChanged)
    {
        var prefix = selectionChanged ? "Cierre seleccionado correctamente.\n\n" : string.Empty;
        return prefix
            + $"Bazar: {closure.BazarName}\n"
            + $"Cierre [{closure.ClosureEventId}] {closure.Description}\n"
            + $"Fecha límite: {closure.PaymentDeadline.ToString("dd/MM/yyyy", Culture)}\n"
            + $"Clientes con comprobante: {closure.ProofReceivedCount}\n"
            + $"Clientes pendientes por pagar: {closure.PendingCount}";
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
        var candidates = BuildPhoneCandidates(phone);
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

        if (text.Contains("firma"))
            return CustomerIntent.Signatures;

        if (text.Contains("pendiente") || text.Contains("pago") || text.Contains("link"))
            return CustomerIntent.PendingPayments;

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

    private static List<string> BuildPhoneCandidates(string? phone)
    {
        var digits = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(digits))
            return new List<string>();

        var candidates = new HashSet<string>(StringComparer.Ordinal) { digits };

        if (digits.Length == 10)
            candidates.Add("52" + digits);
        else if (digits.StartsWith("52", StringComparison.Ordinal) && digits.Length > 10)
            candidates.Add(digits[2..]);

        return candidates.OrderByDescending(x => x.Length).ToList();
    }

    private enum CustomerIntent
    {
        None = 0,
        PendingPayments = 1,
        Signatures = 2,
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

        return text.ToUpperInvariant();
    }
}