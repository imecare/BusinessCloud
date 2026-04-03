namespace BusinessCloud.Domain.Common;

public abstract class BaseAuditableEntity
{

    public int TenantId { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}