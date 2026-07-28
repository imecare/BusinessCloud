using FluentValidation;

namespace BusinessCloud.Application.Bazares.Commands.QuickCreateBzaCustomer;

public class QuickCreateBzaCustomerValidator : AbstractValidator<QuickCreateBzaCustomerCommand>
{
    public QuickCreateBzaCustomerValidator()
    {
        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("El nombre del cliente es requerido.")
            .MaximumLength(200).WithMessage("El nombre no puede superar los 200 caracteres.");
    }
}
