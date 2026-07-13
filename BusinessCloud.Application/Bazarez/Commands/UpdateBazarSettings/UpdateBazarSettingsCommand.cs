using MediatR;
using Microsoft.EntityFrameworkCore;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;

namespace BusinessCloud.Application.Bazares.Commands.UpdateBazarSettings;

public record ContactPhoneInput(string PhoneNumber, string? Label, int ContactType);

public record FacebookProfileInput(string? Name, string ProfileUrl);

/// <summary>
/// Actualiza (upsert) la configuración general del bazar. Los teléfonos y perfiles
/// de Facebook se reemplazan por completo con las listas enviadas.
/// </summary>
public record UpdateBazarSettingsCommand(
    string? BazarName,
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
    List<ContactPhoneInput> Phones,
    List<FacebookProfileInput> FacebookProfiles) : IRequest;

public class UpdateBazarSettingsHandler(IBazaresDbContext context)
    : IRequestHandler<UpdateBazarSettingsCommand>
{
    public async Task Handle(UpdateBazarSettingsCommand request, CancellationToken ct)
    {
        var settings = await context.BazarSettings
            .Include(s => s.ContactPhones)
            .Include(s => s.FacebookProfiles)
            .FirstOrDefaultAsync(ct);

        if (settings is null)
        {
            settings = new BzaBazarSettings();
            context.BazarSettings.Add(settings);
        }

        settings.BazarName = Clean(request.BazarName);
        settings.PhysicalAddress = Clean(request.PhysicalAddress);
        settings.FacebookPageUrl = Clean(request.FacebookPageUrl);
        settings.PrimaryColor = CleanColor(request.PrimaryColor);
        settings.SecondaryColor = CleanColor(request.SecondaryColor);
        settings.LabelTagline = CleanTagline(request.LabelTagline);
        settings.SalesWhatsApp = CleanPhone(request.SalesWhatsApp);
        settings.GeneralWhatsApp = CleanPhone(request.GeneralWhatsApp);
        settings.SecondaryWhatsApp = CleanPhone(request.SecondaryWhatsApp);
        settings.SecondaryWhatsAppDescription = Clean(request.SecondaryWhatsAppDescription);
        settings.SecondaryWhatsAppShowInProof = request.SecondaryWhatsAppShowInProof;
        settings.WithdrawalWithoutCardEnabled = request.WithdrawalWithoutCardEnabled;
        settings.WithdrawalWithoutCardMessage = Clean(request.WithdrawalWithoutCardMessage);
        settings.PaymentCutoffTime = CleanTime(request.PaymentCutoffTime);

        // Reemplazo completo de teléfonos.
        context.ContactPhones.RemoveRange(settings.ContactPhones);
        settings.ContactPhones = (request.Phones ?? new())
            .Where(p => !string.IsNullOrWhiteSpace(p.PhoneNumber))
            .Select(p => new BzaContactPhone
            {
                PhoneNumber = p.PhoneNumber.Trim(),
                Label = Clean(p.Label),
                ContactType = p.ContactType == BzaContactPhoneType.WhatsAppOnly
                    ? BzaContactPhoneType.WhatsAppOnly
                    : BzaContactPhoneType.General,
            })
            .ToList();

        // Reemplazo completo de perfiles de Facebook.
        context.FacebookProfiles.RemoveRange(settings.FacebookProfiles);
        settings.FacebookProfiles = (request.FacebookProfiles ?? new())
            .Where(p => !string.IsNullOrWhiteSpace(p.ProfileUrl))
            .Select(p => new BzaFacebookProfile
            {
                Name = Clean(p.Name),
                ProfileUrl = p.ProfileUrl.Trim(),
            })
            .ToList();

        await context.SaveChangesAsync(ct);
    }

    private static string? Clean(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Acepta solo colores hex válidos (#RGB o #RRGGBB); en otro caso devuelve null.</summary>
    private static string? CleanColor(string? value)
    {
        var v = value?.Trim();
        if (string.IsNullOrEmpty(v))
            return null;
        return System.Text.RegularExpressions.Regex.IsMatch(v, "^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})$")
            ? v.ToUpperInvariant()
            : null;
    }

    /// <summary>Frase del pie de etiqueta: recortada y limitada a 40 caracteres (una línea).</summary>
    private const int TaglineMaxLength = 40;
    private static string? CleanTagline(string? value)
    {
        var v = value?.Trim();
        if (string.IsNullOrEmpty(v))
            return null;
        return v.Length > TaglineMaxLength ? v[..TaglineMaxLength] : v;
    }

    /// <summary>Teléfono/WhatsApp: conserva solo dígitos; null si queda vacío.</summary>
    private static string? CleanPhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var digits = new string(value.Where(char.IsDigit).ToArray());
        return string.IsNullOrEmpty(digits) ? null : digits;
    }

    /// <summary>Hora en formato HH:mm (24h); null si no es válida.</summary>
    private static string? CleanTime(string? value)
    {
        var v = value?.Trim();
        if (string.IsNullOrEmpty(v))
            return null;
        return System.Text.RegularExpressions.Regex.IsMatch(v, "^([01]?[0-9]|2[0-3]):[0-5][0-9]$")
            ? v.PadLeft(5, '0')
            : null;
    }
}
