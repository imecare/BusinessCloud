using FluentValidation;

namespace BusinessCloud.Application.Bazares.Commands.UpdateBzaDelivery;

public class UpdateBzaDeliveryValidator : AbstractValidator<UpdateBzaDeliveryCommand>
{
    public UpdateBzaDeliveryValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("El ID de la entrega es requerido.");

        RuleFor(x => x.DeliveryDate)
            .NotEmpty().WithMessage("La fecha de entrega es requerida.");

        RuleFor(x => x.Status)
            .InclusiveBetween(1, 4).WithMessage("Status debe estar entre 1 y 4.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Las notas no pueden exceder 500 caracteres.");
    }
}
