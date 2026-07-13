using FluentValidation;
using BusinessCloud.Application.Bazares.Common;

namespace BusinessCloud.Application.Bazares.Commands.CreatePaymentCard;

public class CreatePaymentCardValidator : AbstractValidator<CreatePaymentCardCommand>
{
    public CreatePaymentCardValidator()
    {
        RuleFor(x => x.CardNumber)
            .NotEmpty().WithMessage("El número de tarjeta es obligatorio.")
            .MaximumLength(50)
            .Must(CardNumberValidator.IsValid)
            .WithMessage("El número de tarjeta no tiene un formato válido.");

        RuleFor(x => x.CardHolderName)
            .NotEmpty().WithMessage("El nombre del titular es obligatorio.")
            .MaximumLength(150);

        RuleFor(x => x.Bank).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(250);
    }
}
