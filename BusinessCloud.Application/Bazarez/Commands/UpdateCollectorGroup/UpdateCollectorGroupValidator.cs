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
    }
}
