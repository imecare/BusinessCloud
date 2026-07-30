using FluentValidation;
using BusinessCloud.Application.Bazares.Common;

namespace BusinessCloud.Application.Bazares.Commands.CreateBzaCustomer;

public class CreateBzaCustomerValidator : AbstractValidator<CreateBzaCustomerCommand>
{
    public CreateBzaCustomerValidator()
    {
        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("El nombre del cliente es requerido.");

        RuleFor(v => v.BzaCollectorId)
            .GreaterThan(0).WithMessage("Debes asignar un recolector válido.");

        // El teléfono solo es obligatorio cuando el cliente SÍ tiene WhatsApp.
        // Si se marca "sin número", el sistema le asigna un placeholder automáticamente.
        RuleFor(v => v.Phone)
            .MinimumLength(10).WithMessage("El teléfono debe tener al menos 10 dígitos.")
            .When(v => !v.HasNoWhatsApp);

        RuleFor(v => v.FacebookName)
            .Must(v => string.IsNullOrWhiteSpace(v) || FacebookMessengerProfile.IsValidUrl(v))
            .WithMessage("El Facebook debe ser la URL completa del perfil o Messenger (ej. https://facebook.com/usuario o https://m.me/usuario).")
            .Must(v => string.IsNullOrWhiteSpace(v) || FacebookMessengerProfile.IsValid(v))
            .WithMessage("La URL de Facebook debe incluir un usuario o ID válido.");
    }
}