using FluentValidation;
using BusinessCloud.Application.Bazares.Common;

namespace BusinessCloud.Application.Bazares.Commands.UpdateBzaCustomer;

public class UpdateBzaCustomerValidator : AbstractValidator<UpdateBzaCustomerCommand>
{
    public UpdateBzaCustomerValidator()
    {
        RuleFor(v => v.Id).NotEmpty();
        RuleFor(v => v.Name).NotEmpty().MaximumLength(200);
        // El teléfono solo es obligatorio cuando el cliente SÍ tiene WhatsApp.
        RuleFor(v => v.Phone).NotEmpty().MinimumLength(10).When(v => !v.HasNoWhatsApp);
        RuleFor(v => v.BzaCollectorId).GreaterThan(0).WithMessage("Debe tener un recolector asignado.");
        RuleFor(v => v.Status).InclusiveBetween(0, 1).WithMessage("El status debe ser 1 (Activo) o 0 (Inactivo).");
        RuleFor(v => v.FacebookName)
            .Must(v => string.IsNullOrWhiteSpace(v) || FacebookMessengerProfile.IsValidUrl(v))
            .WithMessage("El Facebook debe ser la URL completa del perfil o Messenger (ej. https://facebook.com/usuario o https://m.me/usuario).")
            .Must(v => string.IsNullOrWhiteSpace(v) || FacebookMessengerProfile.IsValid(v))
            .WithMessage("La URL de Facebook debe incluir un usuario o ID válido.");
    }
}