namespace BusinessCloud.Application.Admin.Dtos;

/// <summary>
/// Resumen de una empresa (Tenant) con el estado de su suscripción para el listado admin.
/// </summary>
public class CompanyListItemDto
{
    public string TenantId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public IReadOnlyList<string> Modules { get; set; } = new List<string>();

    public bool HasSubscription { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "MXN";

    public DateTime? PaidUntil { get; set; }
    public int GraceDays { get; set; }
    public DateTime? GraceEndsOn { get; set; }

    /// <summary>Estado calculado: Active | ExpiringSoon | Grace | Suspended.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Días para vencer (positivo) o vencidos (negativo).</summary>
    public int DaysUntilExpiration { get; set; }

    public string? OwnerName { get; set; }
    public string? OwnerPhone { get; set; }
    public int? SellerId { get; set; }
}
