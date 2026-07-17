namespace BusinessCloud.Application.Admin.Dtos;

/// <summary>
/// Detalle completo de una empresa y su suscripción para el panel admin.
/// </summary>
public class CompanyDetailDto
{
    public string TenantId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public IReadOnlyList<string> Modules { get; set; } = new List<string>();
    public int UserCount { get; set; }

    public bool HasSubscription { get; set; }
    public int? SubscriptionId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "MXN";

    public DateTime? StartDate { get; set; }
    public DateTime? PaidUntil { get; set; }
    public int GraceDays { get; set; }
    public DateTime? GraceEndsOn { get; set; }
    public bool IsManuallySuspended { get; set; }

    public string Status { get; set; } = string.Empty;
    public int DaysUntilExpiration { get; set; }

    public string? OwnerName { get; set; }
    public string? OwnerPhone { get; set; }
    public int? SellerId { get; set; }
    public decimal CommissionInitialAmount { get; set; }
    public decimal CommissionMonthlyPercent { get; set; }
    public string? Notes { get; set; }

    public int MessagesAvailable { get; set; }
    public int MessagesTotalPurchased { get; set; }
    public int MessagesTotalUsed { get; set; }
}
