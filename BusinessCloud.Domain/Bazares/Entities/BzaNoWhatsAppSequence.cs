using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Bazares.Entities;

/// <summary>
/// Contador consecutivo, por bazar (TenantId), de los números placeholder de 10
/// dígitos que se asignan a los clientes marcados como "sin número de WhatsApp".
/// Es monótono: nunca reutiliza números aunque un cliente se elimine o luego
/// obtenga un teléfono real.
/// </summary>
public class BzaNoWhatsAppSequence : BaseAuditableEntity
{
    /// <summary>Último número consecutivo asignado en este bazar (0 = ninguno todavía).</summary>
    public int LastNumber { get; set; }
}