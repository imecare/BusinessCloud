using FluentValidation;

namespace BusinessCloud.Application.Payments.Commands.UpdateCustomer;

public class UpdateCustomerValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("El Id del cliente es obligatorio.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(200).WithMessage("El nombre no puede superar 200 caracteres.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("El apellido es obligatorio.")
            .MaximumLength(200).WithMessage("El apellido no puede superar 200 caracteres.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("El teléfono es obligatorio.")
            .Matches(@"^\+?\d{7,15}$").WithMessage("El teléfono debe tener entre 7 y 15 dígitos (opcional '+').");

        RuleFor(x => x.RFC)
            .MaximumLength(13).WithMessage("El RFC no puede superar 13 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.RFC));

        RuleFor(x => x.SellerId)
            .GreaterThanOrEqualTo(0).WithMessage("SellerId debe ser mayor o igual a 0.");
    }
}
