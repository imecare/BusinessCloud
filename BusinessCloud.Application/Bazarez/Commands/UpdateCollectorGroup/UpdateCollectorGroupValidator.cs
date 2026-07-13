using FluentValidation;

namespace BusinessCloud.Application.Bazares.Commands.UpdateCollectorGroup;

public class UpdateCollectorGroupValidator : AbstractValidator<UpdateCollectorGroupCommand>
{
    public UpdateCollectorGroupValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("El Id debe ser mayor a 0");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("La descripción es requerida")
            .MaximumLength(200).WithMessage("La descripción no puede exceder 200 caracteres");

        RuleFor(x => x.DeliveryDay)
            .InclusiveBetween(0, 6).WithMessage("El día de entrega debe estar entre 0 (Domingo) y 6 (Sábado)")
            .When(x => x.DeliveryDay.HasValue);
    }
}
