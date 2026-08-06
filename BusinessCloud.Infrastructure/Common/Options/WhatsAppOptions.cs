namespace BusinessCloud.Infrastructure.Common.Options;

/// <summary>
/// Configuracion de la integracion con WhatsApp Cloud API (Meta).
/// El AccessToken debe almacenarse como secreto (user-secrets / variable de entorno),
/// nunca en el control de versiones.
/// </summary>
public class WhatsAppOptions
{
    public const string SectionName = "WhatsApp";

    public string ApiVersion { get; set; } = "v21.0";
    public string PhoneNumberId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;

    /// <summary>
    /// Numero publico del WhatsApp del bazar o del canal de soporte.
    /// Se usa para crear enlaces wa.me en flujos de recuperacion.
    /// </summary>
    public string PublicNumber { get; set; } = string.Empty;

    /// <summary>
    /// Codigo de pais por defecto (solo digitos, sin '+') que se antepone a los numeros
    /// que se envian sin el (por ejemplo, 10 digitos nacionales). Por defecto Mexico (52).
    /// </summary>
    public string DefaultCountryCode { get; set; } = "52";

    public string? OtpTemplateName { get; set; }

    public string OtpTemplateLang { get; set; } = "es";

    public string? ClosureTotalsTemplateName { get; set; }

    public string ClosureTotalsTemplateLang { get; set; } = "es";

    public string? WebhookVerifyToken { get; set; }

    public string PublicPortalBaseUrl { get; set; } = "https://bazares.bcloud.com.mx";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(PhoneNumberId) && !string.IsNullOrWhiteSpace(AccessToken);
}
