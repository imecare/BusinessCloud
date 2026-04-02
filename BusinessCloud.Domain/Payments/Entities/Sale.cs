using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Payments.Entities
{
    public class Sale : IAuditableEntity
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int SellerId { get; set; }

        public decimal TotalAmount { get; set; }
        public decimal ProductCost { get; set; } // Propósito: Saber ganancia real 
        public decimal CommissionAmount { get; set; } // Monto para el ayudante 

        public bool IsCommissionPaid { get; set; } // ¿Dueño ya pagó comisión? 
        public bool IsPaid { get; set; } // ¿Venta liquidada por el cliente?

        public DateTime SaleDate { get; set; }

        // Propiedades de auditoría
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }

        // Relaciones
        public virtual Customer Customer { get; set; } = null!;
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
