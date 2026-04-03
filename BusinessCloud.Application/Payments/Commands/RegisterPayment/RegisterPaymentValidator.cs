using FluentValidation;

namespace BusinessCloud.Application.Payments.Commands.RegisterPayment;

public class RegisterPaymentValidator : AbstractValidator<RegisterPaymentCommand>
{
    public RegisterPaymentValidator()
    {
        RuleFor(x => x.SaleId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("El monto del abono debe ser mayor a cero.");
        RuleFor(x => x.Reference).MaximumLength(100);
    }
}