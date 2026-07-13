using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Bazares.Entities;

/// <summary>
/// Estados de un Evento de Cierre de Venta (Envío de Totales).
/// </summary>
public static class BzaClosureEventStatus
{
    /// <summary>Recién creado: pendiente de pago (sin comprobantes recibidos).</summary>
    public const int PendingPayment = 1;

    /// <summary>Al menos un comprobante recibido, pendiente de validar.</summary>
    public const int ProofReceived = 2;

    /// <summary>Todos los comprobantes validados: venta pagada y lista para etiquetas.</summary>
    public const int Validated = 3;

    /// <summary>Cierre cancelado.</summary>
    public const int Cancelled = 4;
}

/// <summary>
/// Evento de Cierre de Venta generado al "Enviar Totales".
/// Agrupa uno o varios Eventos de Venta (BzaEvent), fija la fecha límite de pago,
/// y registra las fechas de entrega por grupo de recolección y los totales por cliente.
/// </summary>
public class BzaClosureEvent : BaseAuditableEntity
{
    public int Id { get; set; }

    /// <summary>
    /// Descripción armada automáticamente a partir de los nombres de las ventas
    /// que abarca y la fecha de entrega oficial asignada.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de entrega oficial asignada al cierre (opcional, referencia general).
    /// </summary>
    public DateTime? OfficialDeliveryDate { get; set; }

    /// <summary>
    /// Fecha límite de pago aplicada a los eventos incluidos en este cierre.
    /// </summary>
    public DateTime PaymentDeadline { get; set; }

    /// <summary>
    /// Estado del cierre: 1=PendientePago, 2=ComprobanteRecibido, 3=Validado, 4=Cancelado.
    /// </summary>
    public int Status { get; set; } = BzaClosureEventStatus.PendingPayment;

    /// <summary>
    /// Indica que el evento ya entró en proceso de entrega (se imprimieron etiquetas
    /// y/o hoja de despacho). No altera el estado de pago.
    /// </summary>
    public bool InDeliveryProcess { get; set; }

    // ─────────────────────────────────────────────────────────────────────────
    // Navegación
    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>Eventos de venta incluidos en este cierre.</summary>
    public ICollection<BzaClosureEventItem> Items { get; set; } = new List<BzaClosureEventItem>();

    /// <summary>Fechas de entrega por grupo de recolección que participa en el cierre.</summary>
    public ICollection<BzaClosureGroupDelivery> GroupDeliveries { get; set; } = new List<BzaClosureGroupDelivery>();

    /// <summary>Totales y comprobantes por cada cliente incluido en el cierre.</summary>
    public ICollection<BzaClosureCustomerTotal> CustomerTotals { get; set; } = new List<BzaClosureCustomerTotal>();
}
