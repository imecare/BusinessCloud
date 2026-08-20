using FluentValidation;
namespace BusinessCloud.Application.Bazares.Commands.UnifyDeliveryDates;
public class UnifyDeliveryDatesValidator : AbstractValidator<UnifyDeliveryDatesCommand>
{
 public UnifyDeliveryDatesValidator() { RuleFor(x => x.ClosureEventIds).NotEmpty().Must(x => x.Count <= 100); RuleForEach(x => x.ClosureEventIds).GreaterThan(0); RuleFor(x => x.DeliveryDate).Must(d => d.Date >= DateTime.UtcNow.Date).WithMessage("La fecha no puede estar en el pasado."); }
}