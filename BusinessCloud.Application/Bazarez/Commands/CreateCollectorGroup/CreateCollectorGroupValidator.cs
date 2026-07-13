using FluentValidation;

namespace BusinessCloud.Application.Bazares.Commands.CreateCollectorGroup;

public class CreateCollectorGroupValidator : AbstractValidator<CreateCollectorGroupCommand>
{
    public CreateCollectorGroupValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("La descripción es requerida")
            .MaximumLength(200).WithMessage("La descripción no puede exceder 200 caracteres");

        RuleFor(x => x.DeliveryDay)
            .InclusiveBetween(0, 6).WithMessage("El día de entrega debe estar entre 0 (Domingo) y 6 (Sábado)")
            .When(x => x.DeliveryDay.HasValue);
    }
}
