using FluentValidation;

namespace BusinessCloud.Application.Bazares.Commands.CreateBzaSale;

public class CreateBzaSaleValidator : AbstractValidator<CreateBzaSaleCommand>
{
    public CreateBzaSaleValidator()
    {
        RuleFor(v => v.Description)
            .NotEmpty().WithMessage("La descripción del evento es requerida.")
            .MaximumLength(200).WithMessage("La descripción no puede exceder 200 caracteres.");
    }
}