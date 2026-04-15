using FluentValidation;

namespace BusinessCloud.Application.Bazares.Commands.CreateCollector;

public class CreateCollectorValidator : AbstractValidator<CreateCollectorCommand>
{
    public CreateCollectorValidator()
    {
        RuleFor(v => v.Name)
            .MaximumLength(200)
            .NotEmpty().WithMessage("El nombre del recolector es obligatorio.");

        RuleFor(v => v.FacebookName)
            .MaximumLength(200);
    }
}