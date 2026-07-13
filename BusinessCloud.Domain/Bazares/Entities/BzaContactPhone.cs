using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Bazares.Entities;

/// <summary>
/// Teléfono de atención del bazar. Puede marcarse como atención solo por WhatsApp
/// o como atención general.
/// </summary>
public class BzaContactPhone : BaseAuditableEntity
{
    public int Id { get; set; }

    /// <summary>Configuración del bazar a la que pertenece.</summary>
    public int BzaBazarSettingsId { get; set; }
    public BzaBazarSettings BazarSettings { get; set; } = null!;

    /// <summary>Número telefónico (solo dígitos o formato libre).</summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>Etiqueta opcional (ej: "Ventas", "Soporte").</summary>
    public string? Label { get; set; }

    /// <summary>Tipo de atención. Ver <see cref="BzaContactPhoneType"/> (1=Solo WhatsApp, 2=General).</summary>
    public int ContactType { get; set; } = BzaContactPhoneType.General;
}
