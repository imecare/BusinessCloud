namespace BusinessCloud.Domain.Common.Entities
{
    /// <summary>
    /// Compra de un paquete (o de mensajes adicionales) por una empresa. Es el histórico
    /// de las ventas de paquetes controladas desde el módulo admin.
    /// </summary>
    public class PackagePurchase
    {
        public int Id { get; set; }

        public string TenantId { get; set; } = null!;

        /// <summary>Paquete del catálogo (nulo si fue una venta de mensajes adicionales manual).</summary>
        public int? PackageId { get; set; }
        public Package? Package { get; set; }

        public string PackageName { get; set; } = string.Empty;

        /// <summary>Mensajes agregados al saldo de la empresa con esta compra.</summary>
        public int MessagesAdded { get; set; }

        public decimal Price { get; set; }

        public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;
        public string? Note { get; set; }
        public string? CreatedBy { get; set; }
    }

    /// <summary>
    /// Saldo acumulado de mensajes de WhatsApp de una empresa (un solo campo de disponibles),
    /// con acumulado histórico de comprados y usados.
    /// </summary>
    public class TenantMessageBalance
    {
        public int Id { get; set; }
        public string TenantId { get; set; } = null!;
        public Tenant? Tenant { get; set; }

        /// <summary>Mensajes disponibles para uso (acumulables entre contrataciones).</summary>
        public int Available { get; set; }

        public int TotalPurchased { get; set; }
        public int TotalUsed { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
