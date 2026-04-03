// BusinessCloud.Application/Payments/Validators/CreateSaleValidator.cs
using FluentValidation;
using BusinessCloud.Application.Payments.Dtos;

namespace BusinessCloud.Application.Payments.Validators;

public class CreateSaleValidator : AbstractValidator<CreateSaleRequest>
{
    public CreateSaleValidator()
    {
        RuleFor(x => x.TotalAmount)
            .GreaterThan(0).WithMessage("El monto de la venta debe ser mayor a $0.");

        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("El cliente es obligatorio.");
    }
}