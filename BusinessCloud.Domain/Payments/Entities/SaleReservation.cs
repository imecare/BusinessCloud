using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Payments.Entities
{
    /// <summary>
    /// Apartado: reserva de una venta con los mismos datos que una venta,
    /// pero en estado "apartado". Al concretarse se convierte en una Sale
    /// y se elimina de esta tabla. Vive en su propia tabla (Reservations).
    /// </summary>
    public class SaleReservation : BaseAuditableEntity
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int? SellerId { get; set; }

        public decimal TotalAmount { get; set; }
        public decimal CostPrice { get; set; }
        public decimal CommissionAmount { get; set; }

        public string ProductDescription { get; set; } = string.Empty;

        public DateTime Date { get; set; }

        public virtual Customer Customer { get; set; } = null!;
        public virtual Seller? Seller { get; set; }
    }
}
