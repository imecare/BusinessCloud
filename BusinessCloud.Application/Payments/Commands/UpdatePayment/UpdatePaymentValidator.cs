using FluentValidation;

namespace BusinessCloud.Application.Payments.Commands.UpdatePayment;

public class UpdatePaymentValidator : AbstractValidator<UpdatePaymentCommand>
{
    public UpdatePaymentValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("El Id del abono es obligatorio.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("El monto debe ser mayor a 0.");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty().WithMessage("El método de pago es obligatorio.")
            .MaximumLength(50).WithMessage("El método de pago no puede superar 50 caracteres.");
    }
}
