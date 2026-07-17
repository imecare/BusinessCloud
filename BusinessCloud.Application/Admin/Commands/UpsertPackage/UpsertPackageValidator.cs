using FluentValidation;

namespace BusinessCloud.Application.Admin.Commands.UpsertPackage;

public class UpsertPackageValidator : AbstractValidator<UpsertPackageCommand>
{
    public UpsertPackageValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del paquete es obligatorio.")
            .MaximumLength(150);

        RuleFor(x => x.Module)
            .NotEmpty().WithMessage("El módulo/sistema es obligatorio.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("El precio no puede ser negativo.");

        RuleFor(x => x.IncludedMessages)
            .GreaterThanOrEqualTo(0).WithMessage("Los mensajes incluidos no pueden ser negativos.");

        RuleFor(x => x.Currency)
            .NotEmpty().Length(3);
    }
}
