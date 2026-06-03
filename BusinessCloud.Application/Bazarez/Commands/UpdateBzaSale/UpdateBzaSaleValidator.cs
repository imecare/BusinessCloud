using FluentValidation;

namespace BusinessCloud.Application.Bazares.Commands.UpdateBzaSale;

public class UpdateBzaSaleValidator : AbstractValidator<UpdateBzaSaleCommand>
{
    public UpdateBzaSaleValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("El ID de venta es requerido.");

        RuleFor(x => x.Status)
            .InclusiveBetween(1, 5).WithMessage("Status debe estar entre 1 y 5.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("La descripción no puede exceder 500 caracteres.");
    }
}
