using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Payments.Entities
{
    public class Sale : BaseAuditableEntity
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int? SellerId { get; set; }

        public decimal TotalAmount { get; set; }
        public decimal CostPrice { get; set; }
        public decimal CommissionAmount { get; set; }

        public string ProductDescription { get; set; } = string.Empty;

        public bool IsCommissionPaid { get; set; }
        public bool IsPaid { get; set; }

        // Auditoría de pago de comisión
        public DateTime? CommissionPaidAt { get; set; }
        public string? CommissionPaidByUserId { get; set; }
        public string? CommissionPaymentNote { get; set; }

        public DateTime Date { get; set; }

        public virtual Customer Customer { get; set; } = null!;
        public virtual Seller? Seller { get; set; }
        public virtual ICollection<Payment> Payment { get; set; } = new List<Payment>();
    }
}
