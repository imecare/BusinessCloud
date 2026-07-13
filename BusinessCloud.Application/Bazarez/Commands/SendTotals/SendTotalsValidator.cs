using FluentValidation;

namespace BusinessCloud.Application.Bazares.Commands.SendTotals;

public class SendTotalsValidator : AbstractValidator<SendTotalsCommand>
{
    public SendTotalsValidator()
    {
        RuleFor(x => x.EventIds)
            .NotEmpty().WithMessage("Debes seleccionar al menos un evento de venta.");

        RuleFor(x => x.PaymentDeadline)
            .NotEmpty().WithMessage("La fecha límite de pago es obligatoria.");

        RuleForEach(x => x.GroupDeliveries).ChildRules(g =>
        {
            g.RuleFor(d => d.DeliveryDate)
                .NotEmpty().WithMessage("La fecha de entrega del grupo es obligatoria.");
        });
    }
}
