using FluentValidation;

namespace BusinessCloud.Application.Bazares.Commands.UpdateNotificationMessages;

public class UpdateNotificationMessagesValidator : AbstractValidator<UpdateNotificationMessagesCommand>
{
    public UpdateNotificationMessagesValidator()
    {
        RuleFor(x => x.ChargeMessage).MaximumLength(2000);
        RuleFor(x => x.PaymentDueSoonMessage).MaximumLength(2000);
        RuleFor(x => x.PaymentOverdueMessage).MaximumLength(2000);
        RuleFor(x => x.SaleCancelledMessage).MaximumLength(2000);
    }
}
