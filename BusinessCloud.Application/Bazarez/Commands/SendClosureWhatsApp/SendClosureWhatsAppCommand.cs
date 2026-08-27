using BusinessCloud.Application.Bazares.Common;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

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
    string? MessengerText = null,
    string DeliveryStatus = "processing",
    bool InboxNotificationCreated = true);

public class SendClosureWhatsAppHandler(
    IBazaresDbContext context,
    IWhatsAppSender whatsApp,
    IIdentityDbContext identityContext,
    ICurrentUserService currentUser,
    IConfiguration configuration)
    : IRequestHandler<SendClosureWhatsAppCommand, SendClosureWhatsAppResultDto>
{
    private const string ClosureTemplateLanguage = "es_MX";

    public async Task<SendClosureWhatsAppResultDto> Handle(SendClosureWhatsAppCommand request, CancellationToken ct)
    {
        var closure = await context.ClosureEvents
            .Include(c => c.CustomerTotals)
                .ThenInclude(t => t.Customer)
                    .ThenInclude(cu => cu.Collector)
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

        _ = configuration;

        // El conteo de productos por cliente se usa tanto para la plantilla de Meta como para el
        // texto de copia/manual (que SIEMPRE es la última versión), por lo que se carga siempre.
        var closureProducts = await context.SoldProducts
            .Where(p => p.Sale.BzaClosureEventId == closure.Id)
            .OrderBy(p => p.Id)
            .Select(p => new { CustomerId = p.Sale.BzaCustomerId, p.Description })
            .ToListAsync(ct);
        var productCountByCustomer = closureProducts
            .GroupBy(p => p.CustomerId)
            .ToDictionary(g => g.Key, g => g.Count());

        var targets = request.CustomerIds is { Count: > 0 } customerIds
            ? closure.CustomerTotals.Where(t => customerIds.Contains(t.BzaCustomerId)).ToList()
            : closure.CustomerTotals.ToList();

        var targetTotalIds = targets.Select(t => t.Id).ToList();
        var existingInboxTotalIds = await context.CustomerInboxNotifications
            .Where(n => targetTotalIds.Contains(n.BzaClosureCustomerTotalId))
            .Select(n => n.BzaClosureCustomerTotalId)
            .ToHashSetAsync(ct);

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

            // El payload se arma SIEMPRE: su Preview es el texto que se copia a memoria / se
            // manda manual (inbox, Messenger). El envío automático a Meta sí depende del setting.
            var cobroPayload = ClosureTotalsWhatsAppTemplate.Build(
                bazarName,
                name,
                total.TotalAmount,
                effectiveDeliveryDate,
                closure.PaymentDeadline,
                settings?.PaymentCutoffTime,
                closure.Description,
                productCountByCustomer.GetValueOrDefault(total.BzaCustomerId),
                customer?.Collector?.Name,
                total.UploadToken);
            var notificationMessage = cobroPayload.ManualPreview.Replace(
                ClosureTotalsWhatsAppTemplate.UploadLinkPlaceholder,
                uploadUrl,
                StringComparison.Ordinal);

            if (!existingInboxTotalIds.Contains(total.Id))
                AddInboxNotification(context, total, notificationMessage);

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
                var templateBodyParameters = cobroPayload.BodyParameters
                    .Select(parameter => parameter.Replace(
                        ClosureTotalsWhatsAppTemplate.UploadLinkPlaceholder,
                        uploadUrl,
                        StringComparison.Ordinal))
                    .ToArray();
                send = await SendLatestClosureTemplateAsync(
                    whatsApp,
                    phone,
                    cobroPayload,
                    templateBodyParameters,
                    ct);
            }

            // Los no enviados (sin WhatsApp o con fallo, p. ej. plantilla) se acompañan del texto
            // y el Facebook del cliente para que el operador los mande manualmente por Messenger.
            string? facebookName = null;
            if (!send.Success)
            {
                messengerText = notificationMessage;

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
                messengerText,
                noWhatsApp ? "no_whatsapp" : send.Success ? "processing" : "failed",
                true));
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

    private static void AddInboxNotification(
        IBazaresDbContext context,
        BzaClosureCustomerTotal total,
        string message)
    {
        context.CustomerInboxNotifications.Add(new BzaCustomerInboxNotification
        {
            TenantId = total.TenantId,
            BzaCustomerId = total.BzaCustomerId,
            BzaClosureCustomerTotalId = total.Id,
            Title = "Nuevo total de compra",
            Message = message,
            ActionUrl = $"/comprobante/{total.UploadToken}",
        });
    }

    private static async Task<WhatsAppSendResult> SendLatestClosureTemplateAsync(
        IWhatsAppSender whatsApp,
        string phone,
        ClosureTotalsWhatsAppTemplatePayload cobroPayload,
        IReadOnlyList<string> templateBodyParameters,
        CancellationToken ct)
    {
        var result = await whatsApp.SendTemplateWithResultAsync(
            phone,
            ClosureTotalsWhatsAppTemplate.Name,
            ClosureTemplateLanguage,
            templateBodyParameters,
            ct,
            buttonUrlParameter: cobroPayload.ButtonUrlParameter,
            headerParameter: cobroPayload.HeaderParameter);
        if (result.Success)
        {
            return result;
        }

        var originalMessage = string.IsNullOrWhiteSpace(result.ErrorMessage)
            ? "No se pudo enviar la plantilla de WhatsApp."
            : result.ErrorMessage.Trim();
        return result with
        {
            ErrorMessage = $"{originalMessage} Plantilla enviada: {ClosureTotalsWhatsAppTemplate.Name}[{ClosureTemplateLanguage}].",
        };
    }
}
