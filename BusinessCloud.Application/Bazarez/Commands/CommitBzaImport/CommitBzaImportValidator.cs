using FluentValidation;

namespace BusinessCloud.Application.Bazares.Commands.CommitBzaImport;

public class CommitBzaImportValidator : AbstractValidator<CommitBzaImportCommand>
{
    public CommitBzaImportValidator()
    {
        RuleFor(v => v.EventId)
            .GreaterThan(0).WithMessage("El evento es requerido.");

        RuleFor(v => v.Customers)
            .NotEmpty().WithMessage("La importación debe incluir al menos un cliente.");

        RuleForEach(v => v.Customers).Custom((customer, context) =>
        {
            if (customer.CustomerId is null && customer.NewCustomer is null)
            {
                context.AddFailure("Cada registro debe identificar un cliente existente o uno nuevo.");
                return;
            }

            if (customer.NewCustomer is not { } newCustomer || newCustomer.HasNoWhatsApp)
                return;

            var phone = newCustomer.Phone ?? string.Empty;
            if (phone.Length != 10 || !phone.All(char.IsDigit))
                context.AddFailure("NewCustomer.Phone", "El teléfono debe tener exactamente 10 dígitos.");
        });
    }
}