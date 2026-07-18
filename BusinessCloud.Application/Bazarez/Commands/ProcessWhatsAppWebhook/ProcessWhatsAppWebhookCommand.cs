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

        return "Hola. Escribe PENDIENTES para ver tus bazares con adeudos, o LINKS para obtener tus accesos directos de pago.";
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