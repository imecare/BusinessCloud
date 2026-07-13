using MediatR;
using Microsoft.EntityFrameworkCore;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;

namespace BusinessCloud.Application.Bazares.Commands.UpdateNotificationMessages;

public record UpdateNotificationMessagesCommand(
    string ChargeMessage,
    string PaymentDueSoonMessage,
    string PaymentOverdueMessage,
    string SaleCancelledMessage) : IRequest;

public class UpdateNotificationMessagesHandler(IBazaresDbContext context)
    : IRequestHandler<UpdateNotificationMessagesCommand>
{
    public async Task Handle(UpdateNotificationMessagesCommand request, CancellationToken ct)
    {
        var settings = await context.NotificationSettings.FirstOrDefaultAsync(ct);

        if (settings is null)
        {
            settings = new BzaNotificationSettings();
            context.NotificationSettings.Add(settings);
        }

        settings.ChargeMessage = request.ChargeMessage ?? string.Empty;
        settings.PaymentDueSoonMessage = request.PaymentDueSoonMessage ?? string.Empty;
        settings.PaymentOverdueMessage = request.PaymentOverdueMessage ?? string.Empty;
        settings.SaleCancelledMessage = request.SaleCancelledMessage ?? string.Empty;

        await context.SaveChangesAsync(ct);
    }
}
