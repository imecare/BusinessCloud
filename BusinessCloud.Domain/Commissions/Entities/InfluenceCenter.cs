using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Commissions.Entities;

public class InfluenceCenter : IAuditableEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;
    public string RFC { get; set; } = null!;
    public string Email { get; set; } = null!;

    public string? Username { get; set; }
    public string? PasswordHash { get; set; }

    public bool IsActive { get; set; } = true;

    public string Role { get; set; } = "InfluenceCenter";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
