using FluentValidation;

namespace BusinessCloud.Application.Bazares.Commands.UpdateCollector;

public class UpdateCollectorValidator : AbstractValidator<UpdateCollectorCommand>
{
    public UpdateCollectorValidator()
    {
        RuleFor(v => v.Id)
            .NotEmpty();

        RuleFor(v => v.Name)
            .MaximumLength(200)
            .NotEmpty().WithMessage("El nombre es obligatorio para actualizar.");

        RuleFor(v => v.FacebookName)
            .MaximumLength(200);
    }
}