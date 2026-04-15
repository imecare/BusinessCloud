using FluentValidation;

namespace BusinessCloud.Application.Bazares.Commands.CreateBzaCustomer;

public class CreateBzaCustomerValidator : AbstractValidator<CreateBzaCustomerCommand>
{
    public CreateBzaCustomerValidator()
    {
        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("El nombre del cliente es requerido.");

        RuleFor(v => v.BzaCollectorId)
            .GreaterThan(0).WithMessage("Debes asignar un recolector válido.");

        RuleFor(v => v.Phone)
            .MinimumLength(10).WithMessage("El teléfono debe tener al menos 10 dígitos.");
    }
}