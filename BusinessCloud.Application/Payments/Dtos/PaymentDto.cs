namespace BusinessCloud.Application.Payments.Dtos;

public class PaymentDto
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public int PaymentTypeId { get; set; }
}