using FluentValidation;

namespace BusinessCloud.Application.Bazares.Commands.CreateBzaDelivery;

public class CreateBzaDeliveryValidator : AbstractValidator<CreateBzaDeliveryCommand>
{
    public CreateBzaDeliveryValidator()
    {
        RuleFor(x => x.BzaCollectorGroupId)
            .GreaterThan(0).WithMessage("El ID del grupo es requerido.");

        RuleFor(x => x.DeliveryDate)
            .NotEmpty().WithMessage("La fecha de entrega es requerida.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Las notas no pueden exceder 500 caracteres.");
    }
}
