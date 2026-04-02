using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Payments.Entities
{
    public class Payment : IAuditableEntity
    {
        public int Id { get; set; }
        public int SaleId { get; set; }
        public decimal Amount { get; set; } // Monto del abono
        public DateTime Date { get; set; }
        public string PaymentMethod { get; set; } = "Cash"; // Cash, Card, Transfer [cite: 12]

        // Propiedades de auditoría
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }

        public virtual Sale Sale { get; set; } = null!;
    }
}
