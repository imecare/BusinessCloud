using FluentValidation;

namespace BusinessCloud.Application.Admin.Commands.SetCompanyStatus;

public class SetCompanyStatusValidator : AbstractValidator<SetCompanyStatusCommand>
{
    public SetCompanyStatusValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("La empresa (TenantId) es obligatoria.");
    }
}
