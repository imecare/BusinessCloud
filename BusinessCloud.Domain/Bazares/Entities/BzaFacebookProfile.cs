using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Bazares.Entities;

/// <summary>
/// Perfil de Facebook adicional del bazar, para usos futuros en otras partes del sistema.
/// </summary>
public class BzaFacebookProfile : BaseAuditableEntity
{
    public int Id { get; set; }

    /// <summary>Configuración del bazar a la que pertenece.</summary>
    public int BzaBazarSettingsId { get; set; }
    public BzaBazarSettings BazarSettings { get; set; } = null!;

    /// <summary>Nombre o etiqueta del perfil (opcional).</summary>
    public string? Name { get; set; }

    /// <summary>URL del perfil de Facebook.</summary>
    public string ProfileUrl { get; set; } = string.Empty;
}
