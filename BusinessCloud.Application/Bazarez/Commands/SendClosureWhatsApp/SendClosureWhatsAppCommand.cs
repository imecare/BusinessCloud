using BusinessCloud.Application.Bazares.Common;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.SendClosureWhatsApp;

/// <summary>
/// Envía por WhatsApp (Cloud API) el mensaje de cobro a cada cliente del cierre y registra
/// cada envío para dar seguimiento a su entrega vía los webhooks de Meta.
/// </summary>
public record SendClosureWhatsAppCommand(int ClosureEventId, string PortalBaseUrl) : IRequest<SendClosureWhatsAppResultDto>;

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

public class SendClosureWhatsAppHandler(IBazaresDbContext context, IWhatsAppSender whatsApp, IIdentityDbContext identityContext, ICurrentUserService currentUser)
    : IRequestHandler<SendClosureWhatsAppCommand, SendClosureWhatsAppResultDto>
{
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

        foreach (var total in closure.CustomerTotals)
        {
            var customer = total.Customer;
            var phone = new string((customer?.Phone ?? string.Empty).Where(char.IsDigit).ToArray());
            var name = customer?.Name ?? "Cliente";

            result.Total++;

            DateTime? deliveryDate = total.BzaCollectorGroupId.HasValue
                && deliveryByGroup.TryGetValue(total.BzaCollectorGroupId.Value, out var d)
                    ? d
                    : closure.OfficialDeliveryDate;

            var message = ClosureMessageBuilder
                .Build(bazarName, name, total.TotalAmount, deliveryDate, closure.PaymentDeadline, salesWhatsApp)
                .Replace(ClosureMessageBuilder.UploadLinkPlaceholder, $"{baseUrl}/comprobante/{total.UploadToken}");

            WhatsAppSendResult send;
            if (string.IsNullOrEmpty(phone))
            {
                send = new WhatsAppSendResult(false, null, null, "El cliente no tiene teléfono registrado.");
            }
            else
            {
                send = await whatsApp.SendTextWithResultAsync(phone, message, ct);
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

        // Contabiliza los mensajes enviados en el saldo de la empresa (mensajes acumulables).
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
}
