using FluentValidation;

namespace BusinessCloud.Application.Bazares.Commands.DeleteLiveSaleDrafts;

public class DeleteLiveSaleDraftsValidator : AbstractValidator<DeleteLiveSaleDraftsCommand>
{
    public DeleteLiveSaleDraftsValidator()
    {
        RuleFor(x => x.BzaEventId).GreaterThan(0);
        RuleFor(x => x.DraftId).GreaterThan(0).When(x => x.DraftId.HasValue);
    }
}
