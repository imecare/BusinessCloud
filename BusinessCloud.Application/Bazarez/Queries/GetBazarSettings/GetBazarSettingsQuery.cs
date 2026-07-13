using MediatR;
using Microsoft.EntityFrameworkCore;
using BusinessCloud.Application.Common.Interfaces;

namespace BusinessCloud.Application.Bazares.Queries.GetBazarSettings;

public record ContactPhoneDto(int Id, string PhoneNumber, string? Label, int ContactType);

public record FacebookProfileDto(int Id, string? Name, string ProfileUrl);

public record BazarSettingsDto(
    string? BazarName,
    string? LogoUrl,
    string? PhysicalAddress,
    string? FacebookPageUrl,
    string? PrimaryColor,
    string? SecondaryColor,
    string? LabelTagline,
    string? SalesWhatsApp,
    string? GeneralWhatsApp,
    string? SecondaryWhatsApp,
    string? SecondaryWhatsAppDescription,
    bool SecondaryWhatsAppShowInProof,
    bool WithdrawalWithoutCardEnabled,
    string? WithdrawalWithoutCardMessage,
    string? PaymentCutoffTime,
    List<ContactPhoneDto> Phones,
    List<FacebookProfileDto> FacebookProfiles);

public record GetBazarSettingsQuery : IRequest<BazarSettingsDto>;

public class GetBazarSettingsHandler(IBazaresDbContext context)
    : IRequestHandler<GetBazarSettingsQuery, BazarSettingsDto>
{
    public async Task<BazarSettingsDto> Handle(GetBazarSettingsQuery request, CancellationToken ct)
    {
        var settings = await context.BazarSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (settings is null)
        {
            return new BazarSettingsDto(null, null, null, null, null, null, null, null, null, null, null, false, false, null, null, new(), new());
        }

        var phones = await context.ContactPhones
            .AsNoTracking()
            .Where(p => p.BzaBazarSettingsId == settings.Id)
            .OrderBy(p => p.Id)
            .Select(p => new ContactPhoneDto(p.Id, p.PhoneNumber, p.Label, p.ContactType))
            .ToListAsync(ct);

        var profiles = await context.FacebookProfiles
            .AsNoTracking()
            .Where(p => p.BzaBazarSettingsId == settings.Id)
            .OrderBy(p => p.Id)
            .Select(p => new FacebookProfileDto(p.Id, p.Name, p.ProfileUrl))
            .ToListAsync(ct);

        return new BazarSettingsDto(
            settings.BazarName,
            settings.LogoUrl,
            settings.PhysicalAddress,
            settings.FacebookPageUrl,
            settings.PrimaryColor,
            settings.SecondaryColor,
            settings.LabelTagline,
            settings.SalesWhatsApp,
            settings.GeneralWhatsApp,
            settings.SecondaryWhatsApp,
            settings.SecondaryWhatsAppDescription,
            settings.SecondaryWhatsAppShowInProof,
            settings.WithdrawalWithoutCardEnabled,
            settings.WithdrawalWithoutCardMessage,
            settings.PaymentCutoffTime,
            phones,
            profiles);
    }
}
