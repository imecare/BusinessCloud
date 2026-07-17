namespace BusinessCloud.Application.Admin.Dtos;

/// <summary>Comisionista del SaaS con totales de comisiones.</summary>
public class SystemSellerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public decimal DefaultInitialAmount { get; set; }
    public decimal DefaultMonthlyPercent { get; set; }

    public int SalesCount { get; set; }
    public decimal TotalCommission { get; set; }
    public decimal PaidCommission { get; set; }
    public decimal PendingCommission { get; set; }
}

/// <summary>Asiento de comisión de un comisionista.</summary>
public class SellerCommissionDto
{
    public int Id { get; set; }
    public int SystemSellerId { get; set; }
    public string TenantId { get; set; } = null!;
    public string CompanyName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal BaseAmount { get; set; }
    public decimal Percent { get; set; }
    public decimal Amount { get; set; }
    public DateTime PeriodDate { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Notes { get; set; }
}
