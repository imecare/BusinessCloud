using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Bazares.Entities;

/// <summary>
/// Comprobante de entrega (firma o foto de recibido) subido por el bazar para un
/// Evento de Cierre que ya está en proceso de entrega. Puede ser general (aplica a
/// todos los clientes del cierre) o específico de un grupo de recolección
/// (<see cref="BzaCollectorGroupId"/> asignado) — en ese caso solo los clientes de
/// ese grupo lo verán en su comprobante público.
/// </summary>
public class BzaClosureDeliveryProof : BaseAuditableEntity
{
    public int Id { get; set; }

    public int BzaClosureEventId { get; set; }
    public BzaClosureEvent ClosureEvent { get; set; } = null!;

    /// <summary>Grupo de recolección al que aplica este comprobante (null = general, aplica a todos).</summary>
    public int? BzaCollectorGroupId { get; set; }
    public BzaCollectorGroup? CollectorGroup { get; set; }

    /// <summary>URL de la imagen (firma de recibido / foto de entrega) en BlobStorage.</summary>
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>Fecha en que el bazar subió este comprobante.</summary>
    public DateTime UploadedAt { get; set; }
}