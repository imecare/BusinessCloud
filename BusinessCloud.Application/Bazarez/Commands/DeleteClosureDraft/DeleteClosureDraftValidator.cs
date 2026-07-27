using FluentValidation;

namespace BusinessCloud.Application.Bazares.Commands.DeleteClosureDraft;

public class DeleteClosureDraftValidator : AbstractValidator<DeleteClosureDraftCommand>
{
    public DeleteClosureDraftValidator()
    {
        RuleFor(x => x.ClosureEventId)
            .GreaterThan(0)
            .WithMessage("El id del cierre debe ser mayor a 0.");
    }
}
