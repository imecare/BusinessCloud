using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Payments.Entities
{
    public class Sale : BaseAuditableEntity
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int? SellerId { get; set; }

        public decimal TotalAmount { get; set; }
        public decimal CostPrice { get; set; } // Lo que te costó a ti [cite: 8, 26]
        public decimal CommissionAmount { get; set; } // Comisión del ayudante [cite: 8, 27]

        public bool IsCommissionPaid { get; set; } // ¿Dueño ya pagó comisión? 
        public bool IsPaid { get; set; } // ¿Venta liquidada por el cliente?

        public DateTime Date { get; set; }

        // Agrega esta línea para que EF sepa que una venta pertenece a un cliente
        public virtual Customer Customer { get; set; } = null!;

        // Relación con los abonos individuales
        public virtual ICollection<Payment> Payment { get; set; } = new List<Payment>();
    }
}
