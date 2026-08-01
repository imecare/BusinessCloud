using FluentValidation;

namespace BusinessCloud.Application.Bazares.Commands.SaveLiveSaleRow;

public class SaveLiveSaleRowValidator : AbstractValidator<SaveLiveSaleRowCommand>
{
    public SaveLiveSaleRowValidator()
    {
        RuleFor(x => x.BzaEventId).GreaterThan(0);
        RuleFor(x => x.DraftId).GreaterThan(0).When(x => x.DraftId.HasValue);
        RuleFor(x => x.BzaCustomerId).GreaterThan(0).When(x => x.BzaCustomerId.HasValue);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Price).GreaterThan(0);
    }
}
