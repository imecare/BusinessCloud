namespace BusinessCloud.Application.Payments.Dtos;

public class CommissionistSaleDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string ProductDescription { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public bool IsPaid { get; set; }
    public decimal CommissionAmount { get; set; }
    public bool IsCommissionPaid { get; set; }
    public DateTime? CommissionPaidAt { get; set; }
    public List<PaymentDto> Payments { get; set; } = new();
}