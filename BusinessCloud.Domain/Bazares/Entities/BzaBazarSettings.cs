using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Bazares.Entities;

/// <summary>
/// Configuración general del bazar (una por tenant): identidad, contacto y redes.
/// Datos reutilizables en distintas partes del sistema (portal, mensajes, tickets, etc.).
/// </summary>
public class BzaBazarSettings : BaseAuditableEntity
{
    public int Id { get; set; }

    /// <summary>Nombre comercial del bazar.</summary>
    public string? BazarName { get; set; }

    /// <summary>URL del logo del bazar almacenado en Blob Storage (carpeta logos).</summary>
    public string? LogoUrl { get; set; }

    /// <summary>Color primario del bazar (hex, p. ej. #7C3AED). Fondo del nombre en la etiqueta.</summary>
    public string? PrimaryColor { get; set; }

    /// <summary>Color secundario del bazar (hex, p. ej. #111827). Fondo del pie de la etiqueta.</summary>
    public string? SecondaryColor { get; set; }

    /// <summary>Frase corta personalizada al pie de la etiqueta (por defecto "¡Gracias por su compra!").</summary>
    public string? LabelTagline { get; set; }

    /// <summary>WhatsApp de atención a consultas de ventas (solo dígitos con lada). También usado para el link directo en el comprobante.</summary>
    public string? SalesWhatsApp { get; set; }

    /// <summary>WhatsApp para consultas generales del cliente.</summary>
    public string? GeneralWhatsApp { get; set; }

    /// <summary>WhatsApp secundario adicional (opcional).</summary>
    public string? SecondaryWhatsApp { get; set; }

    /// <summary>Descripción del WhatsApp secundario.</summary>
    public string? SecondaryWhatsAppDescription { get; set; }

    /// <summary>Indica si el WhatsApp secundario se muestra en la vista pública del comprobante.</summary>
    public bool SecondaryWhatsAppShowInProof { get; set; }

    /// <summary>Habilita la opción de "retiro sin tarjeta" en el comprobante del cliente.</summary>
    public bool WithdrawalWithoutCardEnabled { get; set; }

    /// <summary>Mensaje que el bazar muestra al cliente sobre el retiro sin tarjeta.</summary>
    public string? WithdrawalWithoutCardMessage { get; set; }

    /// <summary>Hora límite (HH:mm) para recibir pagos el día límite, por defecto para los cierres.</summary>
    public string? PaymentCutoffTime { get; set; }

    /// <summary>Domicilio físico del bazar (opcional).</summary>
    public string? PhysicalAddress { get; set; }

    /// <summary>URL de la página de Facebook principal del bazar (opcional).</summary>
    public string? FacebookPageUrl { get; set; }

    /// <summary>Teléfonos de atención (opcionales).</summary>
    public ICollection<BzaContactPhone> ContactPhones { get; set; } = new List<BzaContactPhone>();

    /// <summary>Perfiles de Facebook adicionales para usos futuros.</summary>
    public ICollection<BzaFacebookProfile> FacebookProfiles { get; set; } = new List<BzaFacebookProfile>();
}

/// <summary>Tipo de teléfono de atención del bazar.</summary>
public static class BzaContactPhoneType
{
    /// <summary>Atención solo por WhatsApp.</summary>
    public const int WhatsAppOnly = 1;

    /// <summary>Atención general (llamadas y WhatsApp).</summary>
    public const int General = 2;
}
