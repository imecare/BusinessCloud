using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Bazares.Entities;

public class BzaCustomerInboxNotification : BaseAuditableEntity
{
    public int Id { get; set; }
    public int BzaCustomerId { get; set; }
    public BzaCustomer Customer { get; set; } = null!;
    public int BzaClosureCustomerTotalId { get; set; }
    public BzaClosureCustomerTotal ClosureCustomerTotal { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ActionUrl { get; set; } = string.Empty;
    public DateTime? ReadAt { get; set; }
}
