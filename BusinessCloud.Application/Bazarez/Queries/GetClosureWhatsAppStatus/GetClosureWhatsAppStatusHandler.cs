using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetClosureWhatsAppStatus;

public class GetClosureWhatsAppStatusHandler(IBazaresDbContext context)
    : IRequestHandler<GetClosureWhatsAppStatusQuery, ClosureWhatsAppStatusDto>
{
    private static readonly TimeSpan DeliveryConfirmationTimeout = TimeSpan.FromMinutes(15);

    public async Task<ClosureWhatsAppStatusDto> Handle(
        GetClosureWhatsAppStatusQuery request,
        CancellationToken cancellationToken)
    {
        var totals = await context.ClosureCustomerTotals
            .AsNoTracking()
            .Where(t => t.BzaClosureEventId == request.ClosureEventId)
            .Select(t => new
            {
                t.Id,
                t.BzaCustomerId,
                CustomerName = t.Customer.Name,
                CustomerPhone = t.Customer.Phone,
                t.Customer.FacebookName,
            })
            .ToListAsync(cancellationToken);

        if (totals.Count == 0)
            throw new KeyNotFoundException("El cierre no existe o no tiene clientes.");

        var totalIds = totals.Select(t => t.Id).ToList();
        var messages = await context.WhatsAppMessages
            .AsNoTracking()
            .Where(m => m.Purpose == "totals"
                && m.BzaClosureCustomerTotalId.HasValue
                && totalIds.Contains(m.BzaClosureCustomerTotalId.Value))
            .ToListAsync(cancellationToken);
        var inbox = await context.CustomerInboxNotifications
            .AsNoTracking()
            .Where(n => totalIds.Contains(n.BzaClosureCustomerTotalId))
            .ToDictionaryAsync(n => n.BzaClosureCustomerTotalId, cancellationToken);

        var now = DateTime.UtcNow;
        var result = new ClosureWhatsAppStatusDto
        {
            ClosureEventId = request.ClosureEventId,
            Total = totals.Count,
        };

        foreach (var total in totals)
        {
            var message = messages
                .Where(m => m.BzaClosureCustomerTotalId == total.Id)
                .OrderByDescending(m => m.SentAt)
                .ThenByDescending(m => m.Id)
                .FirstOrDefault();
            inbox.TryGetValue(total.Id, out var notification);
            var status = ResolveStatus(message?.Status, message?.SentAt, now);

            Increment(result, status);
            if (notification?.ReadAt is not null)
                result.InboxRead++;
            else if (notification is not null)
                result.InboxUnread++;

            result.Items.Add(new ClosureWhatsAppStatusItemDto(
                total.Id,
                total.BzaCustomerId,
                total.CustomerName,
                new string((total.CustomerPhone ?? string.Empty).Where(char.IsDigit).ToArray()),
                total.FacebookName,
                notification?.Message,
                status,
                message?.SentAt,
                message?.StatusUpdatedAt,
                message?.ErrorMessage,
                notification is not null,
                notification?.ReadAt));
        }

        return result;
    }

    private static string ResolveStatus(string? status, DateTime? sentAt, DateTime now)
        => status?.ToLowerInvariant() switch
        {
            "read" => "read",
            "delivered" => "delivered",
            "failed" => "failed",
            "sin_whatsapp" => "no_whatsapp",
            "sent" or "accepted" when sentAt.HasValue && now - sentAt.Value >= DeliveryConfirmationTimeout => "unconfirmed",
            "manual_sent" => "manual_sent",
            "sent" or "accepted" => "processing",
            _ => "not_sent",
        };

    private static void Increment(ClosureWhatsAppStatusDto result, string status)
    {
        switch (status)
        {
            case "read": result.Read++; break;
            case "delivered": result.Delivered++; break;
            case "failed": result.Failed++; break;
            case "no_whatsapp": result.NoWhatsApp++; break;
            case "unconfirmed": result.Unconfirmed++; break;
            case "manual_sent": result.ManualSent++; break;
            default: result.Processing++; break;
        }
    }
}
