using BusinessCloud.Application.Bazares.Common;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetClosureSignatureMessages;

public record GetClosureSignatureMessagesQuery(int ClosureEventId) : IRequest<ClosureSignatureMessagesDto>;

public class ClosureSignatureMessagesDto
{
    public int ClosureEventId { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool Delivered { get; set; }
    public List<ClosureSignatureMessageItemDto> Items { get; set; } = [];
}

public record ClosureSignatureMessageItemDto(
    int ClosureCustomerTotalId,
    int CustomerId,
    string CustomerName,
    string? FacebookName,
    bool HasWhatsApp,
    bool HasMessenger,
    int ProofCount,
    string Message,
    string? CustomerWhatsAppLink,
    string? CustomerMessengerLink,
    bool Sent,
    DateTime? SentAt);

public class GetClosureSignatureMessagesHandler(IBazaresDbContext context)
    : IRequestHandler<GetClosureSignatureMessagesQuery, ClosureSignatureMessagesDto>
{
    public async Task<ClosureSignatureMessagesDto> Handle(
        GetClosureSignatureMessagesQuery request,
        CancellationToken cancellationToken)
    {
        var closureEvent = await context.ClosureEvents
            .AsNoTracking()
            .Include(c => c.DeliveryProofs)
            .Include(c => c.CustomerTotals)
                .ThenInclude(t => t.Customer)
            .FirstOrDefaultAsync(c => c.Id == request.ClosureEventId, cancellationToken)
            ?? throw new KeyNotFoundException("El evento de cierre no existe.");

        var bazarName = await context.BazarSettings
            .AsNoTracking()
            .Select(settings => settings.BazarName)
            .FirstOrDefaultAsync(cancellationToken);

        var totals = closureEvent.CustomerTotals
            .Where(total => total.Status != BzaClosureCustomerTotalStatus.Cancelled)
            .ToList();
        var totalIds = totals.Select(total => total.Id).ToList();
        var signatureMessages = await context.WhatsAppMessages
            .AsNoTracking()
            .Where(message => message.Purpose == "signatures"
                && message.BzaClosureCustomerTotalId.HasValue
                && totalIds.Contains(message.BzaClosureCustomerTotalId.Value))
            .ToListAsync(cancellationToken);

        var items = totals.Select(total =>
        {
            var customer = total.Customer;
            var customerName = customer?.Name ?? string.Empty;
            // Se incluyen TODAS las firmas del cierre para cada cliente (no solo las de su grupo),
            // para que reciba el enlace de todos los comprobantes de entrega subidos.
            var proofUrls = closureEvent.DeliveryProofs
                .OrderBy(proof => proof.UploadedAt)
                .Select(proof => proof.ImageUrl)
                .ToList();
            var phone = customer?.Phone ?? string.Empty;
            var phoneDigits = new string(phone.Where(char.IsDigit).ToArray());
            var hasWhatsApp = customer is not null
                && !customer.HasNoWhatsApp
                && phoneDigits.Length > 0
                && !NoWhatsAppNumber.IsPlaceholder(phone);
            var messengerHandle = FacebookMessengerProfile.Normalize(customer?.FacebookName);
            var sentMessage = signatureMessages
                .Where(message => message.BzaClosureCustomerTotalId == total.Id
                    && string.Equals(message.Status, "manual_sent", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(message => message.StatusUpdatedAt ?? message.SentAt)
                .ThenByDescending(message => message.Id)
                .FirstOrDefault();

            return new ClosureSignatureMessageItemDto(
                total.Id,
                total.BzaCustomerId,
                string.IsNullOrWhiteSpace(customerName) ? "cliente" : customerName,
                customer?.FacebookName,
                hasWhatsApp,
                messengerHandle is not null,
                proofUrls.Count,
                SignatureMessageBuilder.Build(bazarName, customerName, proofUrls),
                hasWhatsApp ? ClosureMessageBuilder.BuildWhatsAppLink(phone) : null,
                messengerHandle is not null ? $"https://m.me/{messengerHandle}" : null,
                sentMessage is not null,
                sentMessage?.StatusUpdatedAt ?? sentMessage?.SentAt);
        })
        .OrderBy(item => item.CustomerName)
        .ToList();

        return new ClosureSignatureMessagesDto
        {
            ClosureEventId = closureEvent.Id,
            Description = closureEvent.Description,
            Delivered = closureEvent.Delivered,
            Items = items,
        };
    }
}
