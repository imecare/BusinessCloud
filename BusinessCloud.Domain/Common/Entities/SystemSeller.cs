namespace BusinessCloud.Domain.Common.Entities
{
    /// <summary>
    /// Comisionista/vendedor del SaaS (nivel plataforma, cross-tenant). Vende sistemas
    /// (suscripciones) a las empresas y cobra un pago inicial por venta más un porcentaje
    /// mensual de la mensualidad de la empresa.
    /// </summary>
    public class SystemSeller
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public bool IsActive { get; set; } = true;

        /// <summary>Pago inicial por defecto por sistema vendido (se puede ajustar por venta).</summary>
        public decimal DefaultInitialAmount { get; set; }

        /// <summary>Porcentaje mensual por defecto sobre la mensualidad (0-100).</summary>
        public decimal DefaultMonthlyPercent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }

        public ICollection<SellerCommission> Commissions { get; set; } = new List<SellerCommission>();
    }
}
