using BusinessCloud.Application.Bazares.Common;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Globalization;

namespace BusinessCloud.Application.Bazares.Commands.SendClosureWhatsApp;

/// <summary>
/// Envia por WhatsApp (Cloud API) el mensaje de cobro a cada cliente del cierre.
/// </summary>
public record SendClosureWhatsAppCommand(
    int ClosureEventId,
    string PortalBaseUrl,
    IReadOnlyList<int>? CustomerIds = null) : IRequest<SendClosureWhatsAppResultDto>;

public class SendClosureWhatsAppResultDto
{
    public int ClosureEventId { get; set; }
    public int Total { get; set; }
    public int Sent { get; set; }
    public int Failed { get; set; }
    public List<SendClosureWhatsAppItemDto> Items { get; set; } = new();
}

public record SendClosureWhatsAppItemDto(
    int ClosureCustomerTotalId,
    int CustomerId,
    string CustomerName,
    string ToPhone,
    bool Sent,
    string? Error);

public class SendClosureWhatsAppHandler(
    IBazaresDbContext context,
    IWhatsAppSender whatsApp,
    IIdentityDbContext identityContext,
    ICurrentUserService currentUser,
    IConfiguration configuration)
    : IRequestHandler<SendClosureWhatsAppCommand, SendClosureWhatsAppResultDto>
{
    private static readonly CultureInfo Culture = new("es-MX");
    private readonly IConfiguration _configuration = configuration;

    public async Task<SendClosureWhatsAppResultDto> Handle(SendClosureWhatsAppCommand request, CancellationToken ct)
    {
        var closure = await context.ClosureEvents
            .Include(c => c.CustomerTotals)
                .ThenInclude(t => t.Customer)
            .Include(c => c.GroupDeliveries)
            .FirstOrDefaultAsync(c => c.Id == request.ClosureEventId, ct)
            ?? throw new KeyNotFoundException("El evento de cierre no existe.");

        var settings = await context.BazarSettings.FirstOrDefaultAsync(ct);
        var bazarName = settings?.BazarName;
        var salesWhatsApp = settings?.SalesWhatsApp;

        var deliveryByGroup = closure.GroupDeliveries
            .GroupBy(g => g.BzaCollectorGroupId)
            .ToDictionary(g => g.Key, g => g.First().DeliveryDate);

        var baseUrl = (request.PortalBaseUrl ?? string.Empty).TrimEnd('/');
        var now = DateTime.UtcNow;
        var result = new SendClosureWhatsAppResultDto { ClosureEventId = closure.Id };

        var targets = request.CustomerIds is { Count: > 0 } customerIds
            ? closure.CustomerTotals.Where(t => customerIds.Contains(t.BzaCustomerId)).ToList()
            : closure.CustomerTotals.ToList();

        foreach (var total in targets)
        {
            var customer = total.Customer;
            var phone = new string((customer?.Phone ?? string.Empty).Where(char.IsDigit).ToArray());
            var name = customer?.Name ?? "Cliente";

            result.Total++;

            DateTime? deliveryDate = total.BzaCollectorGroupId.HasValue
                && deliveryByGroup.TryGetValue(total.BzaCollectorGroupId.Value, out var d)
                    ? d
                    : closure.OfficialDeliveryDate;

            WhatsAppSendResult send;
            if (string.IsNullOrEmpty(phone))
            {
                send = new WhatsAppSendResult(false, null, null, "El cliente no tiene telefono registrado.");
            }
            else
            {
                var uploadUrl = $"{baseUrl}/comprobante/{total.UploadToken}";
                var closureTotalsTemplateName = _configuration["WhatsApp:ClosureTotalsTemplateName"];

                if (!string.IsNullOrWhiteSpace(closureTotalsTemplateName))
                {
                    var templateLang = string.IsNullOrWhiteSpace(_configuration["WhatsApp:ClosureTotalsTemplateLang"])
                        ? "es"
                        : _configuration["WhatsApp:ClosureTotalsTemplateLang"]!;

                    var commonParams = new[]
                    {
                        string.IsNullOrWhiteSpace(bazarName) ? "Bazar" : bazarName.Trim(),
                        name,
                        total.TotalAmount.ToString("C", Culture),
                        FormatLongDate(deliveryDate ?? closure.PaymentDeadline),
                        FormatLongDate(closure.PaymentDeadline),
                    };

                    send = await TrySendClosureTemplateAsync(
                        whatsApp,
                        phone,
                        closureTotalsTemplateName,
                        templateLang,
                        commonParams,
                        uploadUrl,
                        ct);
                }
                else
                {
                    send = new WhatsAppSendResult(false, null, null, "Plantilla de cobro no configurada.");
                }

                if (!send.Success)
                {
                    var message = ClosureMessageBuilder
                        .Build(bazarName, name, total.TotalAmount, deliveryDate, closure.PaymentDeadline, salesWhatsApp)
                        .Replace(ClosureMessageBuilder.UploadLinkPlaceholder, uploadUrl);

                    var fallbackSend = await whatsApp.SendTextWithResultAsync(phone, message, ct);
                    if (fallbackSend.Success)
                    {
                        send = fallbackSend;
                    }
                }
            }

            context.WhatsAppMessages.Add(new BzaWhatsAppMessage
            {
                TenantId = total.TenantId,
                WaMessageId = send.MessageId,
                ToPhone = phone,
                Purpose = "totals",
                BzaCustomerId = total.BzaCustomerId,
                BzaClosureCustomerTotalId = total.Id,
                Status = send.Success ? "sent" : "failed",
                ErrorCode = int.TryParse(send.ErrorCode, out var ec) ? ec : null,
                ErrorMessage = send.ErrorMessage,
                SentAt = now,
            });

            if (send.Success)
                result.Sent++;
            else
                result.Failed++;

            result.Items.Add(new SendClosureWhatsAppItemDto(
                total.Id, total.BzaCustomerId, name, phone, send.Success, send.Success ? null : send.ErrorMessage));
        }

        await context.SaveChangesAsync(ct);

        if (result.Sent > 0)
        {
            var tenantId = currentUser.TenantId;
            if (!string.IsNullOrEmpty(tenantId))
            {
                var balance = await identityContext.TenantMessageBalances
                    .FirstOrDefaultAsync(b => b.TenantId == tenantId, ct);

                if (balance is not null)
                {
                    balance.Available = Math.Max(0, balance.Available - result.Sent);
                    balance.TotalUsed += result.Sent;
                    balance.UpdatedAt = DateTime.UtcNow;
                    await identityContext.SaveChangesAsync(ct);
                }
            }
        }

        return result;
    }

    private static async Task<WhatsAppSendResult> TrySendClosureTemplateAsync(
        IWhatsAppSender whatsApp,
        string phone,
        string templateName,
        string configuredLang,
        IReadOnlyList<string> commonParams,
        string uploadUrl,
        CancellationToken ct)
    {
        var langs = GetLanguageCandidates(configuredLang);
        WhatsAppSendResult last = new(false, null, null, "No se pudo enviar plantilla de WhatsApp.");

        foreach (var lang in langs)
        {
            var withLinkInBody = commonParams.Concat(new[] { uploadUrl }).ToArray();
            var bodyAttempt = await whatsApp.SendTemplateWithResultAsync(phone, templateName, lang, withLinkInBody, ct);
            if (bodyAttempt.Success)
            {
                return bodyAttempt;
            }

            var buttonAttempt = await whatsApp.SendTemplateWithResultAsync(phone, templateName, lang, commonParams, ct, uploadUrl);
            if (buttonAttempt.Success)
            {
                return buttonAttempt;
            }

            last = buttonAttempt;
        }

        return last;
    }

    private static IReadOnlyList<string> GetLanguageCandidates(string configuredLang)
    {
        var langs = new List<string>();

        if (!string.IsNullOrWhiteSpace(configuredLang))
        {
            langs.Add(configuredLang.Trim());
        }

        var normalized = (configuredLang ?? string.Empty).Trim();
        if (normalized.Contains('_'))
        {
            var baseLang = normalized.Split('_')[0];
            if (!string.IsNullOrWhiteSpace(baseLang) && !langs.Contains(baseLang, StringComparer.OrdinalIgnoreCase))
            {
                langs.Add(baseLang);
            }
        }

        if (!langs.Contains("es", StringComparer.OrdinalIgnoreCase))
        {
            langs.Add("es");
        }

        return langs;
    }

    private static string FormatLongDate(DateTime date)
    {
        var text = date.ToString("dddd dd 'de' MMMM", Culture);
        return text.Length > 0 ? char.ToUpper(text[0], Culture) + text[1..] : text;
    }
}
