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

    /// <summary>Clientes marcados como "sin número de WhatsApp": no se les intentó enviar.</summary>
    public int NoWhatsApp { get; set; }

    /// <summary>true si el envío se bloqueó por falta de transacciones (saldo + cortesía).</summary>
    public bool Blocked { get; set; }

    /// <summary>Transacciones disponibles (saldo pagado) tras el envío.</summary>
    public int Available { get; set; }

    /// <summary>Transacciones de cortesía otorgadas/consumidas en este envío.</summary>
    public int CourtesyGranted { get; set; }

    /// <summary>Mensaje explicativo (p. ej. motivo del bloqueo o aviso de cortesía).</summary>
    public string? Message { get; set; }

    public List<SendClosureWhatsAppItemDto> Items { get; set; } = new();
}

public record SendClosureWhatsAppItemDto(
    int ClosureCustomerTotalId,
    int CustomerId,
    string CustomerName,
    string ToPhone,
    bool Sent,
    string? Error,
    bool NoWhatsApp = false,
    string? FacebookName = null,
    string? MessengerText = null);

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

        var deliveryByGroup = closure.GroupDeliveries
            .GroupBy(g => g.BzaCollectorGroupId)
            .ToDictionary(g => g.Key, g => g.First().DeliveryDate);

        var baseUrl = (request.PortalBaseUrl ?? string.Empty).TrimEnd('/');
        var now = DateTime.UtcNow;
        var result = new SendClosureWhatsAppResultDto { ClosureEventId = closure.Id };

        var targets = request.CustomerIds is { Count: > 0 } customerIds
            ? closure.CustomerTotals.Where(t => customerIds.Contains(t.BzaCustomerId)).ToList()
            : closure.CustomerTotals.ToList();

        // --- Presupuesto de transacciones (saldo pagado + cortesía) ---
        var tenantId = currentUser.TenantId;
        var balance = string.IsNullOrEmpty(tenantId)
            ? null
            : await identityContext.TenantMessageBalances
                .FirstOrDefaultAsync(b => b.TenantId == tenantId, ct);

        // Solo consumen transacción los totales que aún no fueron cobrados (evita cobro doble en reintentos).
        var pendingCharge = targets.Count(t => !t.TransactionCharged);
        var available = balance?.Available ?? 0;
        var courtesyLeft = Math.Max(0, TransactionPolicy.CourtesyLimit - (balance?.CourtesyUsed ?? 0));

        if (pendingCharge > available + courtesyLeft)
        {
            result.Total = targets.Count;
            result.Blocked = true;
            result.Available = available - (balance?.CourtesyUsed ?? 0);
            result.Message =
                $"No tienes transacciones suficientes para este envío. " +
                $"Necesitas {pendingCharge} y solo cuentas con {available} disponibles" +
                (courtesyLeft > 0 ? $" más {courtesyLeft} de cortesía" : string.Empty) +
                ". Contrata más transacciones para continuar.";
            return result;
        }

        var chargedThisSend = 0;

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

            var uploadUrl = $"{baseUrl}/comprobante/{total.UploadToken}";
            var effectiveDeliveryDate = deliveryDate ?? closure.PaymentDeadline;

            WhatsAppSendResult send;
            bool noWhatsApp = customer?.HasNoWhatsApp == true;
            string? messengerText = null;

            if (noWhatsApp)
            {
                // Cliente sin WhatsApp: NO se intenta enviar a Meta. Se reporta para que
                // el operador le mande el mensaje manualmente por Messenger.
                send = new WhatsAppSendResult(false, null, null, "El cliente está marcado como sin WhatsApp.");
            }
            else if (string.IsNullOrEmpty(phone))
            {
                send = new WhatsAppSendResult(false, null, null, "El cliente no tiene telefono registrado.");
            }
            else
            {
                var closureTotalsTemplateName = _configuration["WhatsApp:ClosureTotalsTemplateName"];

                if (!string.IsNullOrWhiteSpace(closureTotalsTemplateName))
                {
                    var templateLang = string.IsNullOrWhiteSpace(_configuration["WhatsApp:ClosureTotalsTemplateLang"])
                        ? "es"
                        : _configuration["WhatsApp:ClosureTotalsTemplateLang"]!;

                    var headerParam = string.IsNullOrWhiteSpace(bazarName) ? "Bazar" : bazarName.Trim();
                    var bodyParams = new[]
                    {
                        name,
                        total.TotalAmount.ToString("N2", Culture),
                        FormatLongDate(effectiveDeliveryDate),
                        FormatLongDate(closure.PaymentDeadline),
                    };

                    send = await TrySendClosureTemplateAsync(
                        whatsApp,
                        phone,
                        closureTotalsTemplateName,
                        templateLang,
                        headerParam,
                        bodyParams,
                        uploadUrl,
                        ct);
                }
                else
                {
                    send = new WhatsAppSendResult(false, null, null, "Plantilla de cobro no configurada.");
                }
            }

            // Los no enviados (sin WhatsApp o con fallo, p. ej. plantilla) se acompañan del texto
            // y el Facebook del cliente para que el operador los mande manualmente por Messenger.
            string? facebookName = null;
            if (!send.Success)
            {
                messengerText = BuildMessengerText(
                    name,
                    string.IsNullOrWhiteSpace(bazarName) ? "el bazar" : bazarName!.Trim(),
                    total.TotalAmount,
                    effectiveDeliveryDate,
                    closure.PaymentDeadline,
                    uploadUrl);
                facebookName = customer?.FacebookName;
            }

            context.WhatsAppMessages.Add(new BzaWhatsAppMessage
            {
                TenantId = total.TenantId,
                WaMessageId = send.MessageId,
                ToPhone = phone,
                Purpose = "totals",
                BzaCustomerId = total.BzaCustomerId,
                BzaClosureCustomerTotalId = total.Id,
                Status = noWhatsApp ? "sin_whatsapp" : (send.Success ? "sent" : "failed"),
                ErrorCode = int.TryParse(send.ErrorCode, out var ec) ? ec : null,
                ErrorMessage = send.ErrorMessage,
                SentAt = now,
            });

            if (noWhatsApp)
                result.NoWhatsApp++;
            else if (send.Success)
                result.Sent++;
            else
                result.Failed++;

            // Se cobra 1 transacción cuando el total efectivamente sale (WhatsApp con éxito o
            // manual "sin WhatsApp"), y solo una vez por cliente/cierre. Los fallos no cobran.
            if ((send.Success || noWhatsApp) && !total.TransactionCharged)
            {
                total.TransactionCharged = true;
                chargedThisSend++;
            }

            result.Items.Add(new SendClosureWhatsAppItemDto(
                total.Id, total.BzaCustomerId, name, phone, send.Success,
                send.Success ? null : send.ErrorMessage,
                noWhatsApp,
                facebookName,
                messengerText));
        }

        // Consumo del saldo: primero las transacciones pagadas y luego la cortesía.
        if (balance is not null && chargedThisSend > 0)
        {
            var fromPaid = Math.Min(chargedThisSend, balance.Available);
            var fromCourtesy = chargedThisSend - fromPaid;
            balance.Available -= fromPaid;
            balance.CourtesyUsed += fromCourtesy;
            balance.TotalUsed += chargedThisSend;
            balance.UpdatedAt = DateTime.UtcNow;
            result.CourtesyGranted = fromCourtesy;
        }

        result.Available = (balance?.Available ?? 0) - (balance?.CourtesyUsed ?? 0);

        await context.SaveChangesAsync(ct);
        if (balance is not null && chargedThisSend > 0)
        {
            await identityContext.SaveChangesAsync(ct);
        }

        return result;
    }

    private static async Task<WhatsAppSendResult> TrySendClosureTemplateAsync(
        IWhatsAppSender whatsApp,
        string phone,
        string templateName,
        string configuredLang,
        string headerParam,
        IReadOnlyList<string> bodyCommonParams,
        string uploadUrl,
        CancellationToken ct)
    {
        var langs = GetLanguageCandidates(configuredLang);
        WhatsAppSendResult last = new(false, null, null, "No se pudo enviar la plantilla de WhatsApp.");

        // total_compra_v2: el nombre del bazar va en el HEADER y el cuerpo lleva 5 parametros
        // (cliente, total, fecha de entrega, fecha limite y el enlace del comprobante).
        var bodyParams = bodyCommonParams.Concat(new[] { uploadUrl }).ToArray();

        foreach (var lang in langs)
        {
            var attempt = await whatsApp.SendTemplateWithResultAsync(
                phone, templateName, lang, bodyParams, ct, headerParameter: headerParam);
            if (attempt.Success)
            {
                return attempt;
            }

            last = attempt;
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

    /// <summary>
    /// Texto plano equivalente a la notificación de cobro, para que el operador lo
    /// copie y lo envíe manualmente por Messenger a los clientes sin WhatsApp.
    /// </summary>
    private static string BuildMessengerText(
        string name, string bazarName, decimal totalAmount,
        DateTime deliveryDate, DateTime paymentDeadline, string uploadUrl)
    {
        var total = totalAmount.ToString("N2", Culture);
        return
            $"Hola {name}, te escribimos de {bazarName}. " +
            $"El total de tu compra es de ${total}. " +
            $"Fecha de entrega: {FormatLongDate(deliveryDate)}. " +
            $"Fecha límite de pago: {FormatLongDate(paymentDeadline)}. " +
            $"Sube tu comprobante de pago aquí: {uploadUrl}";
    }
}
