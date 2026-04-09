
using FluentValidation;

namespace BusinessCloud.Application.Payments.Commands.CreateCustomer;

public class CreateSellerValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateSellerValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(200).WithMessage("El nombre no puede superar 200 caracteres.");

        RuleFor(x => x.LastName)
        .NotEmpty().WithMessage("El nombre es obligatorio.")
        .MaximumLength(200).WithMessage("El nombre no puede superar 200 caracteres.");

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