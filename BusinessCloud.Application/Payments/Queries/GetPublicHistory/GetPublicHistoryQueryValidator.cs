using FluentValidation;

namespace BusinessCloud.Application.Payments.Queries.GetPublicHistory;

public class GetPublicHistoryQueryValidator : AbstractValidator<GetPublicHistoryQuery>
{
    public GetPublicHistoryQueryValidator()
    {
        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("El teléfono es obligatorio.")
            .Matches(@"^\+?\d{7,15}$").WithMessage("El teléfono debe tener entre 7 y 15 dígitos.");

        RuleFor(x => x.Rfc)
            .NotEmpty().WithMessage("El RFC es obligatorio.")
            .MinimumLength(10).WithMessage("El RFC debe tener al menos 10 caracteres.")
            .MaximumLength(13).WithMessage("El RFC no puede superar 13 caracteres.");

        RuleFor(x => x.CompanyCode)
            .NotEmpty().WithMessage("El código de empresa es obligatorio.")
            .MaximumLength(50).WithMessage("El código de empresa no puede superar 50 caracteres.");
    }
}