namespace BusinessCloud.Domain.Common.Entities
{
    /// <summary>Tipo de comisión generada para un comisionista del sistema.</summary>
    public enum CommissionType
    {
        /// <summary>Pago inicial único por la venta del sistema.</summary>
        Initial = 1,

        /// <summary>Porcentaje mensual sobre el pago de la empresa.</summary>
        Monthly = 2,
    }

    /// <summary>
    /// Asiento del libro de comisiones: una comisión devengada a favor de un comisionista
    /// por la venta de un sistema (inicial) o por un pago mensual de la empresa (mensual).
    /// </summary>
    public class SellerCommission
    {
        public int Id { get; set; }

        public int SystemSellerId { get; set; }
        public SystemSeller? Seller { get; set; }

        /// <summary>Empresa (Tenant) que originó la comisión.</summary>
        public string TenantId { get; set; } = null!;

        public CommissionType Type { get; set; }

        /// <summary>Base sobre la que se calculó (el pago de la empresa, en comisiones mensuales).</summary>
        public decimal BaseAmount { get; set; }

        /// <summary>Porcentaje aplicado (para comisiones mensuales).</summary>
        public decimal Percent { get; set; }

        /// <summary>Importe de la comisión a pagar al comisionista.</summary>
        public decimal Amount { get; set; }

        /// <summary>Fecha del periodo/venta que originó la comisión.</summary>
        public DateTime PeriodDate { get; set; } = DateTime.UtcNow;

        public bool IsPaid { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? PaidBy { get; set; }

        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
