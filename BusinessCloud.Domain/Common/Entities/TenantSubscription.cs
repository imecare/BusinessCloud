namespace BusinessCloud.Domain.Common.Entities
{
    /// <summary>
    /// Periodicidad de facturación de una suscripción.
    /// El valor numérico representa la cantidad de meses que cubre cada pago.
    /// </summary>
    public enum BillingPeriod
    {
        Monthly = 1,
        Quarterly = 3,
        Biannual = 6,
        Yearly = 12,
    }

    /// <summary>
    /// Estado calculado de una suscripción respecto a la fecha de pago y la prórroga.
    /// </summary>
    public enum SubscriptionStatus
    {
        /// <summary>Vigente (pagada y con margen amplio).</summary>
        Active = 1,

        /// <summary>Vigente pero próxima a vencer (dentro de la ventana de aviso).</summary>
        ExpiringSoon = 2,

        /// <summary>Venció la fecha pagada pero sigue dentro del periodo de prórroga.</summary>
        Grace = 3,

        /// <summary>Suspendida: venció la prórroga o fue suspendida manualmente.</summary>
        Suspended = 4,
    }

    /// <summary>
    /// Suscripción de una empresa (Tenant) al SaaS: controla hasta qué fecha pagó,
    /// el periodo de prórroga y el estado que determina si sus usuarios pueden operar.
    /// Vive en el IdentityDbContext junto con <see cref="Tenant"/> (no está sujeta al
    /// filtro multi-tenant porque la administra el rol global PlatformAdmin).
    /// </summary>
    public class TenantSubscription
    {
        public int Id { get; set; }

        /// <summary>Empresa a la que pertenece la suscripción (1:1 con Tenant).</summary>
        public string TenantId { get; set; } = null!;
        public Tenant? Tenant { get; set; }

        // --- Plan ---
        public string PlanName { get; set; } = "Mensual";
        public BillingPeriod Period { get; set; } = BillingPeriod.Monthly;

        /// <summary>Precio del periodo contratado (mensualidad/anualidad).</summary>
        public decimal Price { get; set; }
        public string Currency { get; set; } = "MXN";

        // --- Fechas ---
        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        /// <summary>Fecha (inclusive) hasta la que la empresa tiene pagado el servicio.</summary>
        public DateTime PaidUntil { get; set; }

        /// <summary>Días de prórroga tras <see cref="PaidUntil"/> antes de suspender.</summary>
        public int GraceDays { get; set; } = 5;

        // --- Estado manual ---
        /// <summary>Suspensión forzada por el administrador, independientemente de la fecha.</summary>
        public bool IsManuallySuspended { get; set; }

        // --- Comisionista que vendió el sistema (se usa en el módulo de comisiones) ---
        public int? SellerId { get; set; }

        /// <summary>Pago inicial pactado con el comisionista por esta venta.</summary>
        public decimal CommissionInitialAmount { get; set; }

        /// <summary>Porcentaje mensual pactado sobre la mensualidad de la empresa (0-100).</summary>
        public decimal CommissionMonthlyPercent { get; set; }

        // --- Contacto del dueño para avisos por WhatsApp ---
        public string? OwnerName { get; set; }
        public string? OwnerPhone { get; set; }

        public string? Notes { get; set; }

        /// <summary>
        /// Fecha (UTC) del último aviso de vencimiento enviado por WhatsApp al dueño.
        /// Evita reenviar el mismo aviso varias veces el mismo día.
        /// </summary>
        public DateTime? LastExpirationNotifiedOn { get; set; }

        // --- Auditoría ---
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }

        /// <summary>Fecha en la que expira la prórroga (PaidUntil + GraceDays).</summary>
        public DateTime GraceEndsOn => PaidUntil.Date.AddDays(GraceDays);

        /// <summary>
        /// Calcula el estado de la suscripción para una fecha dada.
        /// </summary>
        /// <param name="nowUtc">Momento de evaluación (UTC).</param>
        /// <param name="expiringSoonDays">Ventana de aviso previo al vencimiento.</param>
        public SubscriptionStatus EvaluateStatus(DateTime nowUtc, int expiringSoonDays = 10)
        {
            if (IsManuallySuspended)
                return SubscriptionStatus.Suspended;

            var today = nowUtc.Date;
            var paid = PaidUntil.Date;

            if (today <= paid)
            {
                return (paid - today).TotalDays <= expiringSoonDays
                    ? SubscriptionStatus.ExpiringSoon
                    : SubscriptionStatus.Active;
            }

            return today <= GraceEndsOn
                ? SubscriptionStatus.Grace
                : SubscriptionStatus.Suspended;
        }

        /// <summary>
        /// Indica si los usuarios de la empresa pueden operar (vigente o en prórroga).
        /// </summary>
        public bool AllowsAccess(DateTime nowUtc)
        {
            var status = EvaluateStatus(nowUtc);
            return status != SubscriptionStatus.Suspended;
        }

        /// <summary>
        /// Días restantes (positivo) o vencidos (negativo) respecto a <see cref="PaidUntil"/>.
        /// </summary>
        public int DaysUntilExpiration(DateTime nowUtc) =>
            (int)(PaidUntil.Date - nowUtc.Date).TotalDays;
    }
}
