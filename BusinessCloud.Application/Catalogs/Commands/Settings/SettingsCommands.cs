using System.Text.RegularExpressions;
using BusinessCloud.Application.Catalogs.Common;
using BusinessCloud.Application.Catalogs.Dtos;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Catalogs.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Catalogs.Commands.Settings;

public record UpdateStoreSettingsCommand(StoreSettingsInput Input) : IRequest<StoreSettingsDto>;

public partial class UpdateStoreSettingsHandler(
    ICatalogsDbContext db, IIdentityDbContext identity, ICurrentUserService currentUser)
    : IRequestHandler<UpdateStoreSettingsCommand, StoreSettingsDto>
{
    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex ColorRegex();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    public async Task<StoreSettingsDto> Handle(UpdateStoreSettingsCommand request, CancellationToken ct)
    {
        var tenantId = currentUser.GetRequiredTenantId();
        var input = request.Input;
        var settings = await CatalogSettingsProvider.GetOrCreateAsync(db, identity, tenantId, ct);

        var slug = (input.Slug ?? string.Empty).Trim().ToLowerInvariant();
        if (!CatalogSlug.IsValid(slug))
            throw new ArgumentException("El código del link debe tener de 3 a 60 caracteres: letras minúsculas, números y guiones.");

        var name = CatalogText.Clean(input.PublicName, 120)
            ?? throw new ArgumentException("El nombre público es obligatorio.");

        var color = string.IsNullOrWhiteSpace(input.PrimaryColor) ? settings.PrimaryColor : input.PrimaryColor.Trim();
        if (!ColorRegex().IsMatch(color))
            throw new ArgumentException("El color principal debe tener formato #RRGGBB.");

        var titleColor = input.TitleColor?.Trim() ?? settings.TitleColor;
        if (!ColorRegex().IsMatch(titleColor))
            throw new ArgumentException("El color del nombre de la tienda debe tener formato #RRGGBB.");

        if (input.TitleFont is not null && !Enum.GetNames<CatTitleFont>().Contains(input.TitleFont))
            throw new ArgumentException("Selecciona una tipografía válida para el nombre de la tienda.");

        var currency = string.IsNullOrWhiteSpace(input.Currency) ? settings.Currency : input.Currency.Trim().ToUpperInvariant();
        if (currency.Length != 3 || !currency.All(char.IsLetter))
            throw new ArgumentException("La moneda debe ser un código de 3 letras (p. ej. MXN).");

        var email = CatalogText.Clean(input.OrderEmail, 200);
        if (email is not null && !EmailRegex().IsMatch(email))
            throw new ArgumentException("El correo de pedidos no es válido.");

        var promoIds = (input.PromoCategoryIds ?? []).Distinct().ToList();
        if (promoIds.Count > 2)
            throw new ArgumentException("El panel promocional admite máximo 2 categorías.");
        if (promoIds.Count > 0)
        {
            var found = await db.Categories.CountAsync(c => c.TenantId == tenantId && promoIds.Contains(c.Id), ct);
            if (found != promoIds.Count)
                throw new KeyNotFoundException("Alguna categoría del panel promocional no existe.");
        }
        if (input.PromoPanelEnabled && promoIds.Count != 2)
            throw new ArgumentException("Para activar el panel promocional selecciona 2 categorías.");

        if (slug != settings.Slug
            && await db.StoreSettings.IgnoreQueryFilters().AnyAsync(s => s.Slug == slug && s.Id != settings.Id, ct))
            throw new InvalidOperationException("Ese código de link ya está en uso. Elige otro.");

        settings.Slug = slug;
        settings.PublicName = name;
        settings.PrimaryColor = color;
        settings.TitleColor = titleColor;
        if (input.TitleFont is not null)
            settings.TitleFont = Enum.Parse<CatTitleFont>(input.TitleFont);
        settings.Currency = currency;
        settings.OrderEmail = email;
        settings.Phone = string.IsNullOrWhiteSpace(input.Phone) ? null : CatalogText.NormalizePhone(input.Phone);
        settings.Whatsapp = string.IsNullOrWhiteSpace(input.Whatsapp) ? null : CatalogText.NormalizePhone(input.Whatsapp);
        settings.OrderDisclaimer = CatalogText.Clean(input.OrderDisclaimer, 500);
        settings.PromoPanelEnabled = input.PromoPanelEnabled;
        settings.PromoCategory1Id = promoIds.ElementAtOrDefault(0) is var a and > 0 ? a : null;
        settings.PromoCategory2Id = promoIds.ElementAtOrDefault(1) is var b and > 0 ? b : null;

        await db.SaveChangesAsync(ct);
        return CatalogMappers.ToDto(settings);
    }
}

public record UploadLogoCommand(Stream Content) : IRequest<UploadResultDto>;

public class UploadLogoHandler(
    ICatalogsDbContext db, IIdentityDbContext identity, ICurrentUserService currentUser, IBlobStorageService blob)
    : IRequestHandler<UploadLogoCommand, UploadResultDto>
{
    public async Task<UploadResultDto> Handle(UploadLogoCommand request, CancellationToken ct)
    {
        var tenantId = currentUser.GetRequiredTenantId();
        var settings = await CatalogSettingsProvider.GetOrCreateAsync(db, identity, tenantId, ct);

        var (url, blobName) = await CatalogImageUploader.UploadAsync(blob, tenantId, "logo", request.Content, ct);
        var previous = settings.LogoBlobName;
        settings.LogoUrl = url;
        settings.LogoBlobName = blobName;
        await db.SaveChangesAsync(ct);

        await CatalogImageUploader.DeleteQuietlyAsync(blob, previous, ct);
        return new UploadResultDto(url);
    }
}
