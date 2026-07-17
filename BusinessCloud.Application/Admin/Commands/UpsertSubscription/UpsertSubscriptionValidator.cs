using FluentValidation;

namespace BusinessCloud.Application.Admin.Commands.UpsertSubscription;

public class UpsertSubscriptionValidator : AbstractValidator<UpsertSubscriptionCommand>
{
    public UpsertSubscriptionValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("La empresa (TenantId) es obligatoria.");

        RuleFor(x => x.PlanName)
            .NotEmpty().WithMessage("El nombre del plan es obligatorio.")
            .MaximumLength(100);

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("El precio no puede ser negativo.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3).WithMessage("La moneda debe ser un código ISO de 3 letras (p. ej. MXN).");

        RuleFor(x => x.GraceDays)
            .InclusiveBetween(0, 60).WithMessage("Los días de prórroga deben estar entre 0 y 60.");

        RuleFor(x => x.OwnerPhone)
            .Matches(@"^\d{10,15}$")
            .When(x => !string.IsNullOrWhiteSpace(x.OwnerPhone))
            .WithMessage("El teléfono del dueño debe tener entre 10 y 15 dígitos.");
    }
}
