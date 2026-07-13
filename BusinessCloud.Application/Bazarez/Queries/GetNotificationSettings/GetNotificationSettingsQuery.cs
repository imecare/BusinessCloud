using MediatR;
using Microsoft.EntityFrameworkCore;
using BusinessCloud.Application.Common.Interfaces;

namespace BusinessCloud.Application.Bazares.Queries.GetNotificationSettings;

public record PaymentCardDto(int Id, string CardNumber, string CardHolderName, string? Bank, string? Notes, bool IsActive, bool WasSentForPayment);

public record NotificationSettingsDto(
    string ChargeMessage,
    string PaymentDueSoonMessage,
    string PaymentOverdueMessage,
    string SaleCancelledMessage,
    List<PaymentCardDto> Cards);

public record GetNotificationSettingsQuery(bool IncludeInactiveCards = true)
    : IRequest<NotificationSettingsDto>;

public class GetNotificationSettingsHandler(IBazaresDbContext context)
    : IRequestHandler<GetNotificationSettingsQuery, NotificationSettingsDto>
{
    public async Task<NotificationSettingsDto> Handle(GetNotificationSettingsQuery request, CancellationToken ct)
    {
        var settings = await context.NotificationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        var cardsQuery = context.PaymentCards.AsNoTracking();
        if (!request.IncludeInactiveCards)
        {
            cardsQuery = cardsQuery.Where(c => c.IsActive);
        }

        var cards = await cardsQuery
            .OrderByDescending(c => c.IsActive)
            .ThenBy(c => c.Id)
            .Select(c => new PaymentCardDto(c.Id, c.CardNumber, c.CardHolderName, c.Bank, c.Notes, c.IsActive, c.WasSentForPayment))
            .ToListAsync(ct);

        return new NotificationSettingsDto(
            settings?.ChargeMessage ?? string.Empty,
            settings?.PaymentDueSoonMessage ?? string.Empty,
            settings?.PaymentOverdueMessage ?? string.Empty,
            settings?.SaleCancelledMessage ?? string.Empty,
            cards);
    }
}
