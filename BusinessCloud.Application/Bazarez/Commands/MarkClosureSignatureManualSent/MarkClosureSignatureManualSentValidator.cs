using FluentValidation;

namespace BusinessCloud.Application.Bazares.Commands.MarkClosureSignatureManualSent;

public class MarkClosureSignatureManualSentValidator : AbstractValidator<MarkClosureSignatureManualSentCommand>
{
    public MarkClosureSignatureManualSentValidator()
    {
        RuleFor(command => command.ClosureCustomerTotalId).GreaterThan(0);
    }
}
