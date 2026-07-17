using FluentValidation;

namespace BusinessCloud.Application.Admin.Commands.UpsertSystemSeller;

public class UpsertSystemSellerValidator : AbstractValidator<UpsertSystemSellerCommand>
{
    public UpsertSystemSellerValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del comisionista es obligatorio.")
            .MaximumLength(200);

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("El correo no es válido.");

        RuleFor(x => x.Phone)
            .Matches(@"^\d{10,15}$")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone))
            .WithMessage("El teléfono debe tener entre 10 y 15 dígitos.");

        RuleFor(x => x.DefaultInitialAmount)
            .GreaterThanOrEqualTo(0).WithMessage("El pago inicial no puede ser negativo.");

        RuleFor(x => x.DefaultMonthlyPercent)
            .InclusiveBetween(0, 100).WithMessage("El porcentaje mensual debe estar entre 0 y 100.");
    }
}
