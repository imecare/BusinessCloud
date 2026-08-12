using BusinessCloud.Domain.Payments.Entities;
using FluentValidation;

namespace BusinessCloud.Application.Payments.Commands.CreateExpense;

public class CreateExpenseValidator : AbstractValidator<CreateExpenseCommand>
{
    public CreateExpenseValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("La descripción es obligatoria.")
            .MaximumLength(500).WithMessage("La descripción no puede superar 500 caracteres.");

        RuleFor(x => x.Cost)
            .GreaterThan(0).WithMessage("El costo debe ser mayor a 0.");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("La fecha es obligatoria.");

        RuleFor(x => x.PaymentType)
            .Must(pt => pt == ExpensePaymentTypes.Cash || pt == ExpensePaymentTypes.Installments)
            .WithMessage("La forma de pago debe ser 'Cash' o 'Installments'.");

        RuleFor(x => x.Months)
            .NotNull().WithMessage("Indica el número de meses.")
            .GreaterThanOrEqualTo(2).WithMessage("El número de meses debe ser al menos 2.")
            .LessThanOrEqualTo(60).WithMessage("El número de meses no puede superar 60.")
            .When(x => x.PaymentType == ExpensePaymentTypes.Installments);
    }
}
