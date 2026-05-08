namespace BusinessCloud.Application.Payments.Dtos;

public class AdminSaleDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int? SellerId { get; set; }
    public string? SellerName { get; set; }
    public string ProductDescription { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal CostPrice { get; set; }
    public bool IsPaid { get; set; }
    public decimal CommissionAmount { get; set; }
    public bool IsCommissionPaid { get; set; }
    public DateTime? CommissionPaidAt { get; set; }

    /// <summary>Suma de pagos con PaymentTypeId == 2 (abonos reales)</summary>
    public decimal PaidAmount { get; set; }

    /// <summary>TotalAmount - PaidAmount (nunca negativo)</summary>
    public decimal RemainingBalance { get; set; }

    /// <summary>0-100</summary>
    public decimal PaymentProgress { get; set; }

    public List<PaymentDto> Payments { get; set; } = new();
}
