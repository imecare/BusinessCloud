namespace BusinessCloud.Infrastructure.Common.Options;

/// <summary>
/// Configuración de la integración con WhatsApp Cloud API (Meta).
/// El AccessToken debe almacenarse como secreto (user-secrets / variable de entorno),
/// nunca en el control de versiones.
/// </summary>
public class WhatsAppOptions
{
    public const string SectionName = "WhatsApp";

    public string ApiVersion { get; set; } = "v21.0";
    public string PhoneNumberId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Código de país por defecto (solo dígitos, sin '+') que se antepone a los números
    /// que se envían sin él (por ejemplo, 10 dígitos nacionales). Por defecto México (52).
    /// </summary>
    public string DefaultCountryCode { get; set; } = "52";

    /// <summary>
    /// Nombre de la plantilla de autenticación aprobada para enviar el código OTP.
    /// Si está vacío, se envía un mensaje de texto (válido en la ventana de 24h / pruebas).
    /// </summary>
    public string? OtpTemplateName { get; set; }

    /// <summary>Idioma de la plantilla OTP (por ejemplo "es" o "es_MX").</summary>
    public string OtpTemplateLang { get; set; } = "es";

    /// <summary>Token de verificación del webhook de Meta (hub.verify_token). Debe coincidir con el configurado en Meta.</summary>
    public string? WebhookVerifyToken { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(PhoneNumberId) && !string.IsNullOrWhiteSpace(AccessToken);
}
