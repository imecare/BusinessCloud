using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Catalogs.Entities;
using BusinessCloud.Domain.Common.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Catalogs.Common;

public static class CatalogSlug
{
    private static readonly Regex ValidSlug = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    public static bool IsValid(string? slug) =>
        !string.IsNullOrEmpty(slug) && slug.Length is >= 3 and <= 60 && ValidSlug.IsMatch(slug);

    /// <summary>Convierte un nombre en slug: minúsculas, sin acentos, separado por guiones.</summary>
    public static string FromName(string? name)
    {
        var normalized = (name ?? string.Empty).Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            var lower = char.ToLowerInvariant(c);
            sb.Append(lower is (>= 'a' and <= 'z') or (>= '0' and <= '9') ? lower : '-');
        }

        var slug = Regex.Replace(sb.ToString(), "-{2,}", "-").Trim('-');
        if (slug.Length > 50) slug = slug[..50].Trim('-');
        return slug.Length >= 3 ? slug : "tienda";
    }

    public static async Task<string> GenerateUniqueAsync(ICatalogsDbContext db, string? name, CancellationToken ct)
    {
        var baseSlug = FromName(name);
        var candidate = baseSlug;
        var n = 2;
        while (await db.StoreSettings.IgnoreQueryFilters().AnyAsync(s => s.Slug == candidate, ct))
        {
            candidate = $"{baseSlug}-{n++}";
        }
        return candidate;
    }
}

public static class CatalogText
{
    public static string? Clean(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }

    /// <summary>Teléfono: solo dígitos y "+" inicial, entre 7 y 15 dígitos.</summary>
    public static string NormalizePhone(string? phone)
    {
        var raw = (phone ?? string.Empty).Trim();
        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.Length is < 7 or > 15)
            throw new ArgumentException("El teléfono debe tener entre 7 y 15 dígitos.");
        return raw.StartsWith('+') ? "+" + digits : digits;
    }

    public static string DefaultDisclaimer(string publicName) =>
        $"Tu pedido será enviado a {publicName}. Verificarán existencia al recibirlo y te confirmarán disponibilidad y total final.";
}

/// <summary>Crea la configuración del catálogo la primera vez (slug generado del nombre de la empresa).</summary>
public static class CatalogSettingsProvider
{
    public static async Task<CatStoreSettings> GetOrCreateAsync(
        ICatalogsDbContext db, IIdentityDbContext identity, string tenantId, CancellationToken ct)
    {
        var settings = await db.StoreSettings.FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);
        if (settings is not null) return settings;

        var tenantName = await identity.Tenants
            .Where(t => t.Id == tenantId)
            .Select(t => t.Name)
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("La empresa no existe.");

        settings = new CatStoreSettings
        {
            TenantId = tenantId,
            PublicName = tenantName,
            Slug = await CatalogSlug.GenerateUniqueAsync(db, tenantName, ct),
        };
        db.StoreSettings.Add(settings);
        await db.SaveChangesAsync(ct);
        return settings;
    }
}

/// <summary>
/// Resuelve el catálogo público a partir del slug y valida que la empresa pueda operar
/// (tenant activo, módulo Catalogs activo y suscripción no suspendida).
/// </summary>
public static class PublicCatalogResolver
{
    public const string UnavailableMessage = "Catálogo no disponible";

    public static async Task<CatStoreSettings> ResolveAsync(
        ICatalogsDbContext db, IIdentityDbContext identity, string slug, CancellationToken ct)
    {
        var normalized = (slug ?? string.Empty).Trim().ToLowerInvariant();
        var settings = await db.StoreSettings.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Slug == normalized, ct)
            ?? throw new KeyNotFoundException(UnavailableMessage);

        var tenantActive = await identity.Tenants.AnyAsync(t => t.Id == settings.TenantId && t.IsActive, ct);
        var moduleActive = await identity.TenantModules.AnyAsync(
            m => m.TenantId == settings.TenantId && m.Module == SystemModules.Catalogs && m.IsActive, ct);
        if (!tenantActive || !moduleActive)
            throw new KeyNotFoundException(UnavailableMessage);

        var subscription = await identity.TenantSubscriptions
            .FirstOrDefaultAsync(s => s.TenantId == settings.TenantId, ct);
        if (subscription is not null && !subscription.AllowsAccess(DateTime.UtcNow))
            throw new KeyNotFoundException(UnavailableMessage);

        return settings;
    }
}
