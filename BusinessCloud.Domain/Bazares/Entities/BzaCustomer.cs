using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Bazares.Entities;

/// <summary>
/// Cliente del bazar. Puede comprar productos en múltiples Eventos de Venta.
/// </summary>
public class BzaCustomer : BaseAuditableEntity
{
    public int Id { get; set; }

    /// <summary>
    /// Nombre completo del cliente.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Nombre de Facebook (opcional).
    /// </summary>
    public string? FacebookName { get; set; }

    /// <summary>
    /// Teléfono de contacto.
    /// </summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Dirección de entrega del cliente.
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Estado del cliente: 1=Activo, 0=Inactivo
    /// </summary>
    public int Status { get; set; } = 1;

    /// <summary>
    /// Token único para portal de auto-gestión del cliente.
    /// </summary>
    public string? PortalToken { get; set; }

    /// <summary>
    /// true cuando el cliente fue dado de alta rápida (solo nombre) durante la captura
    /// en vivo de una venta, y todavía falta completar su teléfono y recolector.
    /// Se limpia automáticamente al editar/completar su información.
    /// </summary>
    public bool IsPendingInfo { get; set; } = false;

    /// <summary>
    /// true cuando el cliente no proporcionó número de WhatsApp. En ese caso el campo
    /// <see cref="Phone"/> guarda un número placeholder de 10 dígitos consecutivo por
    /// bazar (0000000001, 0000000002, …) para respetar la restricción de teléfono
    /// único e irrepetible, pero NO se le intenta enviar WhatsApp. Se limpia cuando el
    /// cliente aporta su teléfono real.
    /// </summary>
    public bool HasNoWhatsApp { get; set; } = false;

    // ─────────────────────────────────────────────────────────────────────────
    // Relaciones
    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// FK al Recolector asignado al cliente.
    /// </summary>
    public int BzaCollectorId { get; set; }
    public BzaCollector Collector { get; set; } = null!;

    /// <summary>
    /// Ventas de este cliente en diferentes Eventos de Venta.
    /// Cada venta agrupa los productos comprados por el cliente en un evento.
    /// </summary>
    public ICollection<BzaSale> Sales { get; set; } = [];

    /// <summary>
    /// Pagos realizados por este cliente en diferentes Eventos de Venta.
    /// </summary>
    public ICollection<BzaPayment> Payments { get; set; } = [];
}