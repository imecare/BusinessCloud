using FluentValidation;

namespace BusinessCloud.Application.Bazares.Commands.MergeBzaCustomers;

public class MergeBzaCustomersValidator : AbstractValidator<MergeBzaCustomersCommand>
{
    public MergeBzaCustomersValidator()
    {
        RuleFor(x => x.SurvivorId)
            .GreaterThan(0)
            .WithMessage("Debes indicar el cliente que se conservará.");

        RuleFor(x => x.MergeIds)
            .NotNull()
            .Must(ids => ids != null && ids.Count >= 1 && ids.Count <= 2)
            .WithMessage("Debes seleccionar entre 2 y 3 clientes para unir.");

        RuleFor(x => x)
            .Must(cmd => cmd.MergeIds != null && !cmd.MergeIds.Contains(cmd.SurvivorId))
            .WithMessage("El cliente que se conserva no puede estar también en la lista de duplicados.")
            .When(x => x.MergeIds != null);

        RuleFor(x => x)
            .Must(cmd => cmd.MergeIds != null && cmd.MergeIds.Distinct().Count() == cmd.MergeIds.Count)
            .WithMessage("Hay clientes duplicados en la selección.")
            .When(x => x.MergeIds != null);

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("El nombre del cliente es obligatorio.");

        RuleFor(x => x.Phone)
            .NotEmpty()
            .WithMessage("El teléfono del cliente es obligatorio.")
            .MinimumLength(10)
            .WithMessage("El teléfono debe tener al menos 10 dígitos.");

        RuleFor(x => x.BzaCollectorId)
            .GreaterThan(0)
            .WithMessage("Debes seleccionar un recolector.");

        RuleFor(x => x.Status)
            .Must(s => s == 0 || s == 1)
            .WithMessage("El estado del cliente no es válido.");
    }
}
