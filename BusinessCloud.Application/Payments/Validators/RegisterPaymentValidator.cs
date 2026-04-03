using FluentValidation;
using BusinessCloud.Application.Payments.Dtos;

namespace BusinessCloud.Application.Payments.Validators
{
    public class RegisterPaymentValidator : AbstractValidator<RegisterPaymentRequest>
    {
        public RegisterPaymentValidator()
        {
            RuleFor(x => x.SaleId)
                .NotEmpty().WithMessage("El ID de venta es obligatorio.")
                .GreaterThan(0).WithMessage("El ID de la venta debe ser mayor que cero.");

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("El monto del pago debe ser mayor que cero.")
                .PrecisionScale(18, 2, false).WithMessage("El formato del monto no es válido.");

            RuleFor(x => x.PaymentMethod)
                .NotEmpty().WithMessage("El método de pago es obligatorio.")
                .MaximumLength(50).WithMessage("El método de pago no puede exceder los 50 caracteres.");
        }
    }
}