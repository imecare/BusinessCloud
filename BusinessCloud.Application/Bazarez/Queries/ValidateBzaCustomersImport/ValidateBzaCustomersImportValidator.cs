using FluentValidation;

namespace BusinessCloud.Application.Bazares.Queries.ValidateBzaCustomersImport;

public class ValidateBzaCustomersImportValidator : AbstractValidator<ValidateBzaCustomersImportQuery>
{
    public ValidateBzaCustomersImportValidator()
    {
        RuleFor(query => query.FileContent)
            .NotEmpty().WithMessage("El archivo de clientes es obligatorio.")
            .Must(content => content.Length <= 20 * 1024 * 1024)
            .WithMessage("El archivo de clientes no puede superar 20 MB.");
    }
}
