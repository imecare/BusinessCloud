using BusinessCloud.Application.Bazares.Common;
using FluentValidation;

namespace BusinessCloud.Application.Bazares.Commands.CommitBzaCustomersImport;

public class CommitBzaCustomersImportValidator : AbstractValidator<CommitBzaCustomersImportCommand>
{
    public CommitBzaCustomersImportValidator()
    {
        RuleFor(command => command.Customers).NotNull();
        RuleFor(command => command.NewCollectors).NotNull();

        RuleForEach(command => command.Customers).ChildRules(customer =>
        {
            customer.RuleFor(item => item.Name)
                .NotEmpty().WithMessage("El nombre del cliente es obligatorio.")
                .MaximumLength(200);
            customer.RuleFor(item => item.CollectorName)
                .NotEmpty().WithMessage("Cada cliente debe tener un recolector real o marcarse como 'Aún sin recolector'.")
                .MaximumLength(200)
                .Must(IsRealCollectorName).WithMessage("El recolector Sin asignar no es válido para importar clientes.")
                .When(item => !item.HasNoCollector);
            customer.RuleFor(item => item.Phone)
                .Must(IsValidOptionalPhone).WithMessage("El WhatsApp debe tener 10 dígitos válidos.");
            customer.RuleFor(item => item.FacebookName)
                .Must(value => string.IsNullOrWhiteSpace(value) || FacebookMessengerProfile.IsValid(value))
                .WithMessage("El Facebook debe contener un usuario, ID o URL válida.");
        });

        RuleForEach(command => command.NewCollectors).ChildRules(collector =>
        {
            collector.RuleFor(item => item.Name)
                .NotEmpty()
                .MaximumLength(200)
                .Must(IsRealCollectorName).WithMessage("No se puede crear el recolector Sin asignar.");
            collector.RuleFor(item => item.GroupId).GreaterThan(0);
        });
    }

    private static bool IsValidOptionalPhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        var normalized = PhoneNumberNormalizer.Normalize(value);
        return normalized.Length == 12
            && normalized.StartsWith(PhoneNumberNormalizer.DefaultCountryCode, StringComparison.Ordinal)
            && normalized[2] != '0';
    }

    private static bool IsRealCollectorName(string? value)
    {
        var key = CollectorCatalogNameNormalizer.ToComparisonKey(value);
        return key.Length > 0 && key != "SIN ASIGNAR";
    }
}
