namespace BusinessCloud.Domain.Common.Entities;

/// <summary>
/// Módulo habilitado para un Tenant.
/// Controla a qué sistemas tiene acceso cada empresa.
/// </summary>
public class TenantModule
{
    public int Id { get; set; }
    public string TenantId { get; set; } = null!;
    public string Module { get; set; } = null!; // "Payments", "Bazares", "Commissions"
    public bool IsActive { get; set; } = true;
    public DateTime ActivatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeactivatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
}
