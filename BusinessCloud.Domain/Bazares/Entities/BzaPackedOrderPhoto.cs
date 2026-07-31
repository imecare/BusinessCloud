using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Bazares.Entities;

/// <summary>Foto del pedido empacado asociada al total de un cliente.</summary>
public class BzaPackedOrderPhoto : BaseAuditableEntity
{
    public int Id { get; set; }
    public int BzaClosureCustomerTotalId { get; set; }
    public BzaClosureCustomerTotal Total { get; set; } = null!;
    public string ImageUrl { get; set; } = string.Empty;
    public string BlobName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
}