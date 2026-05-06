namespace BusinessCloud.Domain.Payments.Entities;

public class DeletedPayment
{
    public int Id { get; set; }

    // Datos originales del pago
    public int OriginalPaymentId { get; set; }
    public int SaleId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string? Reference { get; set; }
    public int PaymentTypeId { get; set; }

    // Auditoría original
    public string? TenantId { get; set; }
    public DateTime? OriginalCreatedAt { get; set; }
    public string? OriginalCreatedBy { get; set; }

    // Auditoría de eliminación
    public DateTime DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public string? DeletedReason { get; set; }
}
