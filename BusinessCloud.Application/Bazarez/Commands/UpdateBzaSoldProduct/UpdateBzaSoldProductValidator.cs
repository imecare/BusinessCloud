using FluentValidation;

namespace BusinessCloud.Application.Bazares.Commands.UpdateBzaSoldProduct;

public class UpdateBzaSoldProductValidator : AbstractValidator<UpdateBzaSoldProductCommand>
{
    public UpdateBzaSoldProductValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("El ID del producto vendido es requerido.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("La descripción del producto es requerida.")
            .MaximumLength(500).WithMessage("La descripción no puede exceder 500 caracteres.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("El precio debe ser mayor a 0.");
    }
}
