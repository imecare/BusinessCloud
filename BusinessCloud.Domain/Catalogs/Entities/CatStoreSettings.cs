using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Catalogs.Entities;

public enum CatTitleFont
{
    Default,
    PlayfairDisplay,
    DancingScript,
    Montserrat,
    CormorantGaramond,
    LibreBaskerville,
    Cinzel,
    AbrilFatface,
    BebasNeue,
    Oswald,
    Pacifico,
    Sacramento
}

/// <summary>
/// Configuración pública del catálogo de una empresa (1:1 con el tenant).
/// El <see cref="Slug"/> arma el link público /c/{slug} y es único globalmente.
/// </summary>
public class CatStoreSettings : BaseAuditableEntity
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string PublicName { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? LogoBlobName { get; set; }
    public string PrimaryColor { get; set; } = "#0d6efd";
    public string TitleColor { get; set; } = "#0f172a";
    public CatTitleFont TitleFont { get; set; } = CatTitleFont.Default;
    public string? Phone { get; set; }
    public string? Whatsapp { get; set; }
    public string? OrderEmail { get; set; }
    public string Currency { get; set; } = "MXN";
    public string? OrderDisclaimer { get; set; }
    public bool PromoPanelEnabled { get; set; }
    public int? PromoCategory1Id { get; set; }
    public int? PromoCategory2Id { get; set; }

    /// <summary>Último consecutivo de pedido emitido para el tenant.</summary>
    public int LastOrderNumber { get; set; }
}
