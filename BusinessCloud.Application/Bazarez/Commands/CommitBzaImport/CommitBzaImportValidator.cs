using FluentValidation;
using BusinessCloud.Application.Bazares.Common;

namespace BusinessCloud.Application.Bazares.Commands.CommitBzaImport;

public class CommitBzaImportValidator : AbstractValidator<CommitBzaImportCommand>
{
    public CommitBzaImportValidator()
    {
        RuleFor(v => v.EventId)
            .GreaterThan(0).WithMessage("El evento es requerido.");

        RuleFor(v => v.Customers)
            .NotEmpty().WithMessage("La importación debe incluir al menos un cliente.");
        RuleForEach(v => v.NewCollectors).ChildRules(collector =>
        {
            collector.RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre del recolector es requerido.")
                .MaximumLength(200).WithMessage("El nombre del recolector no puede exceder 200 caracteres.");
            collector.RuleFor(x => x.GroupId).GreaterThan(0).WithMessage("El grupo del recolector es requerido.");
        });

        RuleForEach(v => v.Customers).Custom((customer, context) =>
        {
            if (customer.CustomerId is null && customer.NewCustomer is null)
            {
                context.AddFailure("Cada registro debe identificar un cliente existente o uno nuevo.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(customer.ChangeFacebookNameTo)
                && !IsValidFacebook(customer.ChangeFacebookNameTo))
            {
                context.AddFailure("ChangeFacebookNameTo", "La URL de Facebook debe incluir un usuario o ID despues de la diagonal.");
            }
            if (customer.NewCustomer is not { } newCustomer)
                return;
            if (!string.IsNullOrWhiteSpace(newCustomer.FacebookName)
                && !IsValidFacebook(newCustomer.FacebookName))
            {
                context.AddFailure("NewCustomer.FacebookName", "La URL de Facebook debe incluir un usuario o ID despues de la diagonal.");
            }

            if (newCustomer.HasNoWhatsApp) return;

            var phone = newCustomer.Phone ?? string.Empty;
            if (phone.Length != 10 || !phone.All(char.IsDigit))
                context.AddFailure("NewCustomer.Phone", "El teléfono debe tener exactamente 10 dígitos.");
        });
    }
    private static bool IsValidFacebook(string value)
        => FacebookMessengerProfile.IsValidUrl(value) && FacebookMessengerProfile.IsValid(value);
}