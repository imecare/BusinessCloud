using FluentValidation;
using BusinessCloud.Application.Bazares.Common;

namespace BusinessCloud.Application.Bazares.Commands.QuickCreateBzaCustomer;

public class QuickCreateBzaCustomerValidator : AbstractValidator<QuickCreateBzaCustomerCommand>
{
    public QuickCreateBzaCustomerValidator()
    {
        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("El nombre del cliente es requerido.")
            .MaximumLength(200).WithMessage("El nombre no puede superar los 200 caracteres.");
        RuleFor(v => v.Phone).MinimumLength(10).When(v => !string.IsNullOrWhiteSpace(v.Phone));
        RuleFor(v => v.BzaCollectorId).GreaterThan(0).When(v => v.BzaCollectorId.HasValue);
        RuleFor(v => v.FacebookName)
            .Must(value => string.IsNullOrWhiteSpace(value) || FacebookMessengerProfile.IsValidUrl(value))
            .WithMessage("El Facebook debe ser la URL completa del perfil o Messenger.")
            .Must(value => string.IsNullOrWhiteSpace(value) || FacebookMessengerProfile.IsValid(value))
            .WithMessage("La URL de Facebook debe incluir un usuario o ID valido.");
    }
}
