using FluentValidation;

namespace BusinessCloud.Application.Bazares.Commands.MarkCustomerNotificationRead;

public class MarkCustomerNotificationReadValidator : AbstractValidator<MarkCustomerNotificationReadCommand>
{
    public MarkCustomerNotificationReadValidator()
    {
        RuleFor(x => x.AccessToken).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NotificationId).GreaterThan(0);
        RuleFor(x => x.AccessKind).IsInEnum();
    }
}
