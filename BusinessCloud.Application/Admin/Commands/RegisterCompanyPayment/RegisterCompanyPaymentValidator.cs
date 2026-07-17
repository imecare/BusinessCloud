using FluentValidation;

namespace BusinessCloud.Application.Admin.Commands.RegisterCompanyPayment;

public class RegisterCompanyPaymentValidator : AbstractValidator<RegisterCompanyPaymentCommand>
{
    public RegisterCompanyPaymentValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("La empresa (TenantId) es obligatoria.");

        RuleFor(x => x.Periods)
            .InclusiveBetween(1, 36).WithMessage("Los periodos deben estar entre 1 y 36.");

        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Amount.HasValue)
            .WithMessage("El monto no puede ser negativo.");
    }
}
