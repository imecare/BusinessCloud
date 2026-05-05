using FluentValidation;

namespace BusinessCloud.Application.Payments.Commands.MarkCommissionPaid;

public class MarkCommissionPaidValidator : AbstractValidator<MarkCommissionPaidCommand>
{
    public MarkCommissionPaidValidator()
    {
        RuleFor(x => x.SaleId)
            .GreaterThan(0).WithMessage("El SaleId es obligatorio.");

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("La nota no puede superar 500 caracteres.")
            .When(x => x.Note is not null);
    }
}