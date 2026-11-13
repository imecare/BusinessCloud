using BusinessCloud.Domain.Common.Entities;
using FluentValidation;

namespace BusinessCloud.Application.Admin.Commands.SetCompanyModules;

public class SetCompanyModulesValidator : AbstractValidator<SetCompanyModulesCommand>
{
    public SetCompanyModulesValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("La empresa (TenantId) es obligatoria.");
        RuleFor(x => x.Modules)
            .NotEmpty().WithMessage("Selecciona al menos una aplicación.");
        RuleForEach(x => x.Modules)
            .Must(module => SystemModules.All.Contains(module))
            .WithMessage("La aplicación '{PropertyValue}' no es válida.");
    }
}
