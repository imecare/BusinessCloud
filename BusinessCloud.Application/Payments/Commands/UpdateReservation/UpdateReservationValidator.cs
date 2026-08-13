using FluentValidation;

namespace BusinessCloud.Application.Payments.Commands.UpdateReservation;

public class UpdateReservationValidator : AbstractValidator<UpdateReservationCommand>
{
    public UpdateReservationValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Id inválido.");

        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("El cliente es obligatorio.");

        RuleFor(x => x.TotalAmount)
            .GreaterThan(0).WithMessage("El monto total debe ser mayor a 0.");

        RuleFor(x => x.CostPrice)
            .GreaterThanOrEqualTo(0).WithMessage("El costo no puede ser negativo.")
            .LessThanOrEqualTo(x => x.TotalAmount)
            .WithMessage("El costo no puede ser mayor al monto total.");

        RuleFor(x => x.CommissionAmount)
            .GreaterThanOrEqualTo(0).WithMessage("La comisión no puede ser negativa.");

        RuleFor(x => x.ProductDescription)
            .NotEmpty().WithMessage("La descripción del producto es obligatoria.")
            .MaximumLength(500).WithMessage("La descripción no puede superar 500 caracteres.");
    }
}
