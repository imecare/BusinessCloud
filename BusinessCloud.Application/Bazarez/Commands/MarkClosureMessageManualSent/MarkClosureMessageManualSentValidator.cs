using FluentValidation;

namespace BusinessCloud.Application.Bazares.Commands.MarkClosureMessageManualSent;

public class MarkClosureMessageManualSentValidator : AbstractValidator<MarkClosureMessageManualSentCommand>
{
    public MarkClosureMessageManualSentValidator()
    {
        RuleFor(x => x.ClosureCustomerTotalId).GreaterThan(0);
    }
}
