using FluentValidation;

namespace BusinessCloud.Application.Bazares.Commands.CreateBzaSoldProduct;

public class CreateBzaSoldProductValidator : AbstractValidator<CreateBzaSoldProductCommand>
{
    public CreateBzaSoldProductValidator()
    {
        RuleFor(x => x.BzaSaleId)
            .GreaterThan(0).WithMessage("El ID del Evento de Venta es requerido.");

        RuleFor(x => x.BzaCustomerId)
            .GreaterThan(0).WithMessage("El ID del Cliente es requerido.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("La descripción del producto es requerida.")
            .MaximumLength(500).WithMessage("La descripción no puede exceder 500 caracteres.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("El precio de venta debe ser mayor a 0.");
    }
}
