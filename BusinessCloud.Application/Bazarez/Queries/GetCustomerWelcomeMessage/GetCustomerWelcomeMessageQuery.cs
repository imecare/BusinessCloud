using BusinessCloud.Application.Bazares.Common;
using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BusinessCloud.Application.Bazares.Queries.GetCustomerWelcomeMessage;

/// <summary>
/// Arma el mensaje de bienvenida para un cliente, en sus dos variantes de canal (WhatsApp y
/// Messenger), e indica qué canales tiene disponibles el cliente y los enlaces para abrir el
/// chat con él. El envío es semi-manual: el bazar copia el texto o abre el chat y lo pega.
/// </summary>
public record GetCustomerWelcomeMessageQuery(int CustomerId) : IRequest<CustomerWelcomeMessageDto>;

public record CustomerWelcomeMessageDto(
    int CustomerId,
    string CustomerName,
    bool HasWhatsApp,
    bool HasMessenger,
    string WhatsAppMessage,
    string MessengerMessage,
    string? CustomerWhatsAppLink,
    string? CustomerMessengerLink);

public class GetCustomerWelcomeMessageHandler(IBazaresDbContext context, IConfiguration configuration)
    : IRequestHandler<GetCustomerWelcomeMessageQuery, CustomerWelcomeMessageDto>
{
    private readonly IBazaresDbContext _context = context;
    private readonly IConfiguration _configuration = configuration;

    public async Task<CustomerWelcomeMessageDto> Handle(
        GetCustomerWelcomeMessageQuery request,
        CancellationToken cancellationToken)
    {
        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken)
            ?? throw new KeyNotFoundException("El cliente no existe.");

        var settings = await _context.BazarSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        var systemNumber = _configuration["WhatsApp:PublicNumber"];
        var bazarWhatsAppLink = ClosureMessageBuilder.BuildWhatsAppLink(settings?.SalesWhatsApp);
        var bazarMessengerLink = BuildMessengerLink(settings?.FacebookPageUrl);

        var whatsAppMessage = WelcomeMessageBuilder.Build(
            WelcomeMessageBuilder.WhatsAppChannel,
            settings?.BazarName,
            customer.Name,
            systemNumber,
            bazarWhatsAppLink,
            bazarMessengerLink,
            settings?.WelcomeMessageComplement);

        var messengerMessage = WelcomeMessageBuilder.Build(
            WelcomeMessageBuilder.MessengerChannel,
            settings?.BazarName,
            customer.Name,
            systemNumber,
            bazarWhatsAppLink,
            bazarMessengerLink,
            settings?.WelcomeMessageComplement);

        var phoneDigits = new string((customer.Phone ?? string.Empty).Where(char.IsDigit).ToArray());
        var hasWhatsApp = !customer.HasNoWhatsApp
            && phoneDigits.Length > 0
            && !NoWhatsAppNumber.IsPlaceholder(customer.Phone);

        var messengerHandle = FacebookMessengerProfile.Normalize(customer.FacebookName);
        var hasMessenger = messengerHandle is not null;

        return new CustomerWelcomeMessageDto(
            customer.Id,
            customer.Name,
            hasWhatsApp,
            hasMessenger,
            whatsAppMessage,
            messengerMessage,
            hasWhatsApp ? ClosureMessageBuilder.BuildWhatsAppLink(customer.Phone) : null,
            messengerHandle is not null ? $"https://m.me/{messengerHandle}" : null);
    }

    private static string? BuildMessengerLink(string? facebookPageUrl)
    {
        var handle = FacebookMessengerProfile.Normalize(facebookPageUrl);
        return handle is not null ? $"https://m.me/{handle}" : null;
    }
}
