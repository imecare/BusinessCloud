using FluentValidation;

namespace BusinessCloud.Application.Payments.Commands.CreateSale;

public class CreateSaleValidator : AbstractValidator<CreateSaleCommand>
{
    public CreateSaleValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("El cliente es obligatorio.");

        RuleFor(x => x.TotalAmount)
            .GreaterThan(0).WithMessage("El monto total debe ser mayor a 0.");

        RuleFor(x => x.CostPrice)
            .GreaterThanOrEqualTo(0).WithMessage("El costo no puede ser negativo.");

        // CommissionAmount <= TotalAmount - CostPrice (margen disponible)
        RuleFor(x => x.CostPrice)
            .LessThanOrEqualTo(x => x.TotalAmount)
            .WithMessage("El costo no puede ser mayor al monto total de la venta.");

        RuleFor(x => x.ProductDescription)
            .NotEmpty().WithMessage("La descripción del producto es obligatoria.")
            .MaximumLength(500).WithMessage("La descripción no puede superar 500 caracteres.");
    }
}