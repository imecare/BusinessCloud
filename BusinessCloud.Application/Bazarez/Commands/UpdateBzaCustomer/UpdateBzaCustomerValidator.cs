using FluentValidation;

namespace BusinessCloud.Application.Bazares.Commands.UpdateBzaCustomer;

public class UpdateBzaCustomerValidator : AbstractValidator<UpdateBzaCustomerCommand>
{
    public UpdateBzaCustomerValidator()
    {
        RuleFor(v => v.Id).NotEmpty();
        RuleFor(v => v.Name).NotEmpty().MaximumLength(200);
        RuleFor(v => v.Phone).NotEmpty().MinimumLength(10);
        RuleFor(v => v.BzaCollectorId).GreaterThan(0).WithMessage("Debe tener un recolector asignado.");
        RuleFor(v => v.Status).InclusiveBetween(0, 1).WithMessage("El status debe ser 1 (Activo) o 0 (Inactivo).");
    }
}