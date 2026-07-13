using FluentValidation;

namespace BusinessCloud.Application.Bazares.Commands.CreateBzaSaleWithProducts;

public class CreateBzaSaleWithProductsValidator : AbstractValidator<CreateBzaSaleWithProductsCommand>
{
    public CreateBzaSaleWithProductsValidator()
    {
        RuleFor(x => x.BzaEventId)
            .GreaterThan(0).WithMessage("El ID del Evento de Venta es requerido.");

        RuleFor(x => x.BzaCustomerId)
            .GreaterThan(0).WithMessage("El ID del Cliente es requerido.");

        RuleFor(x => x.Products)
            .NotEmpty().WithMessage("Debe registrar al menos un producto en la venta.");

        RuleForEach(x => x.Products).ChildRules(product =>
        {
            product.RuleFor(p => p.Description)
                .NotEmpty().WithMessage("La descripción del producto es requerida.")
                .MaximumLength(500).WithMessage("La descripción no puede exceder 500 caracteres.");

            product.RuleFor(p => p.Price)
                .GreaterThan(0).WithMessage("El precio de venta debe ser mayor a 0.");
        });
    }
}
