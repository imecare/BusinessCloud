using BusinessCloud.Application.Bazares.Common;
using FluentValidation;

namespace BusinessCloud.Application.Bazares.Commands.UpdateBazarSettings;

public class UpdateBazarSettingsValidator : AbstractValidator<UpdateBazarSettingsCommand>
{
    public UpdateBazarSettingsValidator()
    {
        RuleFor(v => v.FacebookPageUrl)
            .Must(v => string.IsNullOrWhiteSpace(v) || FacebookMessengerProfile.IsValidUrl(v))
            .WithMessage("La página de Facebook debe ser una URL válida de Facebook o Messenger.");

        RuleForEach(v => v.FacebookProfiles)
            .ChildRules(profile =>
            {
                profile.RuleFor(p => p.ProfileUrl)
                    .Must(v => string.IsNullOrWhiteSpace(v) || FacebookMessengerProfile.IsValidUrl(v))
                    .WithMessage("El perfil adicional debe ser una URL válida de Facebook o Messenger.");
            });
    }
}
