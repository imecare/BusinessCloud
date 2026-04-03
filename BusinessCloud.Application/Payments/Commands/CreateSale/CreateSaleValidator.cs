using FluentValidation;

namespace BusinessCloud.Application.Payments.Commands.CreateSale;

// Valida los datos antes de que lleguen al Handler [cite: 17, 40]
public class CreateSaleValidator : AbstractValidator<CreateSaleCommand>
{
    public CreateSaleValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.TotalAmount).GreaterThan(0).WithMessage("El monto debe ser positivo ");
         RuleFor(x => x.CostPrice).GreaterThan(0).WithMessage("El costo es obligatorio ");
    }
}