
using FluentValidation;

namespace BusinessCloud.Application.Payments.Commands.CreateSeller;

public class CreateSellerValidator : AbstractValidator<CreateSellerCommand>
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
    }
}