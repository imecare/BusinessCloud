using FluentValidation;

namespace BusinessCloud.Application.Payments.Commands.CreateSale;

public class CreateSaleValidator : AbstractValidator<CreateSaleCommand>
{
    public CreateSaleValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThanOrEqualTo(0)
            .When(x => x.IsCashSale)
            .WithMessage("El cliente no es valido.");
        RuleFor(x => x.CustomerId)
            .GreaterThan(0).When(x => !x.IsCashSale)
            .WithMessage("El cliente es obligatorio.");
        RuleFor(x => x.SellerId)
            .Must(id => id.HasValue && id.Value > 0).When(x => x.IsCashSale)
            .WithMessage("El comisionista es obligatorio en ventas de contado.");
        RuleFor(x => x.PaymentMethod)
            .Must(method => method is "Cash" or "Card" or "Transfer").When(x => x.IsCashSale)
            .WithMessage("Seleccione efectivo, tarjeta o transferencia.");
        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().When(x => x.IsCashSale)
            .WithMessage("La clave de registro es obligatoria para ventas de contado.");
        RuleFor(x => x.IdempotencyKey).MaximumLength(100);

        RuleFor(x => x.TotalAmount)
            .GreaterThan(0).WithMessage("El monto total debe ser mayor a 0.")
            .PrecisionScale(18, 2, true);

        RuleFor(x => x.CostPrice)
            .GreaterThanOrEqualTo(0).WithMessage("El costo no puede ser negativo.")
            .PrecisionScale(18, 2, true);

        // CommissionAmount <= TotalAmount - CostPrice (margen disponible)
        RuleFor(x => x.CostPrice)
            .LessThanOrEqualTo(x => x.TotalAmount)
            .WithMessage("El costo no puede ser mayor al monto total de la venta.");

        RuleFor(x => x.CommissionAmount)
            .GreaterThanOrEqualTo(0).WithMessage("La comisión no puede ser negativa.")
            .LessThanOrEqualTo(x => x.TotalAmount - x.CostPrice)
            .WithMessage("La comision no puede superar la ganancia.")
            .PrecisionScale(18, 2, true);

        RuleFor(x => x.ProductDescription)
            .NotEmpty().WithMessage("La descripción del producto es obligatoria.")
            .MaximumLength(500).WithMessage("La descripción no puede superar 500 caracteres.");
    }
}