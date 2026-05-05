using BusinessCloud.Application.Payments.Dtos;

namespace BusinessCloud.Application.Payments.Dtos;

public class PublicHistoryLookupResponse
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public bool HasMovements { get; set; }
    public List<SaleHistoryDto> Sales { get; set; } = new();
}

public class SaleHistoryDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string ProductDescription { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public bool IsPaid { get; set; }
    public List<PaymentDto> Payment { get; set; } = new();
}