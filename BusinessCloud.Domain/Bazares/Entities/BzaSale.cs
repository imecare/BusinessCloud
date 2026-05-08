using BusinessCloud.Domain.Common;
using BusinessCloud.Domain.Common.Entities;

namespace BusinessCloud.Domain.Bazares.Entities;

public class BzaSale : BaseAuditableEntity
{
    public int Id { get; set; }
    public string? Description { get; set; }
    public decimal Total { get; set; }
    /// <summary>
    /// 1=Pendiente, 2=Pagado, 3=ListoParaEntrega, 4=EntregadoARecolector, 5=Cancelado
    /// </summary>
    public int Status { get; set; }
    public DateTime? PaymentDeadline { get; set; }
    public string? ProofOfPaymentUrl { get; set; }
    public DateTime? DeliveredToCollectorAt { get; set; }
    public string? LabelCode { get; set; }
    public string? PortalToken { get; set; }

    public int BzaCustomerId { get; set; }
    public BzaCustomer Customer { get; set; } = null!;
    public ICollection<BzaProduct> Products { get; set; } = new List<BzaProduct>();
    public ICollection<BzaPayment> Payments { get; set; } = new List<BzaPayment>();
}