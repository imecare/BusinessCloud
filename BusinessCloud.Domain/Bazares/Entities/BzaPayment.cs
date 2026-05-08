using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Bazares.Entities;

/// <summary>
/// Pago registrado contra una venta del bazar.
/// </summary>
public class BzaPayment : BaseAuditableEntity
{
    public int Id { get; set; }
    public int BzaSaleId { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string PaymentMethod { get; set; } = string.Empty; // "Efectivo", "Transferencia", "Deposito"
    public string? ProofImageUrl { get; set; }
    public string? Reference { get; set; }
    public bool IsVerified { get; set; }

    public BzaSale Sale { get; set; } = null!;
}
