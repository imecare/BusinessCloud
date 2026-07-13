using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Bazares.Entities;

/// <summary>
/// Comprobante de pago individual subido por el cliente para un total de cierre.
/// Un cliente puede adjuntar varios comprobantes (p. ej. cuando paga en depósitos
/// separados) y todos quedan asociados al mismo <see cref="BzaClosureCustomerTotal"/>.
/// </summary>
public class BzaClosureProof : BaseAuditableEntity
{
    public int Id { get; set; }

    /// <summary>Total de cierre al que pertenece el comprobante.</summary>
    public int BzaClosureCustomerTotalId { get; set; }
    public BzaClosureCustomerTotal Total { get; set; } = null!;

    /// <summary>URL del archivo del comprobante (BlobStorage).</summary>
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>Fecha en que el cliente subió este comprobante.</summary>
    public DateTime UploadedAt { get; set; }
}
