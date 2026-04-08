using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Payments.Entities
{
    public class Payment : BaseAuditableEntity
    {
        public int Id { get; set; }
        public int SaleId { get; set; }
        public decimal Amount { get; set; } // Monto del abono
        public DateTime Date { get; set; }
        public string PaymentMethod { get; set; } = "Cash"; // Cash, Card, Transfer [cite: 12]
        public string Reference { get; set; }
        public int PaymentTypeId { get; set; } = 2;
        public virtual Sale Sale { get; set; } = null!;
    }
}
