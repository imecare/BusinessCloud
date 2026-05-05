using FluentValidation;

namespace BusinessCloud.Application.Payments.Commands.UpdateSellerStatus;

public class UpdateSellerStatusValidator : AbstractValidator<UpdateSellerStatusCommand>
{
    public UpdateSellerStatusValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("El Id del vendedor es obligatorio.");

        RuleFor(x => x.StatusId)
            .InclusiveBetween(0, 1).WithMessage("StatusId debe ser 0 (Inactivo) o 1 (Activo).");
    }
}