namespace BusinessCloud.Domain.Payments.Entities;

public class DeletedSale
{
    public int Id { get; set; }

    // Datos originales de la venta
    public int OriginalSaleId { get; set; }
    public int CustomerId { get; set; }
    public int? SellerId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal CostPrice { get; set; }
    public decimal CommissionAmount { get; set; }
    public string ProductDescription { get; set; } = string.Empty;
    public bool IsCommissionPaid { get; set; }
    public bool IsPaid { get; set; }
    public DateTime Date { get; set; }

    // Auditoría original
    public string? TenantId { get; set; }
    public DateTime? OriginalCreatedAt { get; set; }
    public string? OriginalCreatedBy { get; set; }

    // Auditoría de eliminación
    public DateTime DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public string? DeletedReason { get; set; }
}
