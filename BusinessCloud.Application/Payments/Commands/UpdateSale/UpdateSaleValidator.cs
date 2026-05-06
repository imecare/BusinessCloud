using FluentValidation;

namespace BusinessCloud.Application.Payments.Commands.UpdateSale;

public class UpdateSaleValidator : AbstractValidator<UpdateSaleCommand>
{
    public UpdateSaleValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("El Id de la venta es obligatorio.");

        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("El cliente es obligatorio.");

        RuleFor(x => x.TotalAmount)
            .GreaterThan(0).WithMessage("El monto total debe ser mayor a 0.");

        RuleFor(x => x.CostPrice)
            .GreaterThanOrEqualTo(0).WithMessage("El costo no puede ser negativo.");

        RuleFor(x => x.CostPrice)
            .LessThanOrEqualTo(x => x.TotalAmount)
            .WithMessage("El costo no puede ser mayor al monto total de la venta.");

        RuleFor(x => x.CommissionAmount)
            .GreaterThanOrEqualTo(0).WithMessage("La comisión no puede ser negativa.");

        RuleFor(x => x.ProductDescription)
            .NotEmpty().WithMessage("La descripción del producto es obligatoria.")
            .MaximumLength(500).WithMessage("La descripción no puede superar 500 caracteres.");
    }
}
