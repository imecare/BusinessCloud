using System.ComponentModel.DataAnnotations.Schema;
using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Bazares.Entities;

/// <summary>
/// Origen de una venta (BzaSale).
/// </summary>
public static class BzaSaleSource
{
    /// <summary>Capturada directamente en el sistema (página de Ventas / detalle de evento).</summary>
    public const int DirectCapture = 1;

    /// <summary>Importada desde un archivo Excel.</summary>
    public const int Excel = 2;
}

/// <summary>
/// Venta: agrupa los productos comprados por UN cliente dentro de UN Evento de Venta.
/// Una venta pertenece a un par único (Evento, Cliente) y puede tener 1 o varios
/// productos asociados. El total NO se persiste, se calcula bajo demanda.
/// </summary>
public class BzaSale : BaseAuditableEntity
{
    public int Id { get; set; }

    // ─────────────────────────────────────────────────────────────────────────
    // Relaciones
    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// FK al Evento de Venta (Corte/En Vivo/Catálogo) al que pertenece esta venta.
    /// </summary>
    public int BzaEventId { get; set; }
    public BzaEvent Event { get; set; } = null!;

    /// <summary>
    /// FK al Cliente dueño de esta venta.
    /// </summary>
    public int BzaCustomerId { get; set; }
    public BzaCustomer Customer { get; set; } = null!;

    /// <summary>
    /// Origen de la venta: 1 = Captura directa en sistema, 2 = Importada desde Excel.
    /// </summary>
    public int Source { get; set; } = BzaSaleSource.DirectCapture;

    /// <summary>
    /// Indica si la venta está cerrada. Se cierra cuando el cliente sube su comprobante
    /// de pago tras un envío de totales. Una venta cerrada ya no admite nuevos productos.
    /// </summary>
    public bool IsClosed { get; set; }

    /// <summary>
    /// FK (opcional) al Evento de Cierre (Envío de Totales) que ya incluye esta venta.
    /// Una venta solo puede pertenecer a UN evento de pago: si está asignada, ya no se
    /// puede volver a enviar en otro envío de totales.
    /// </summary>
    public int? BzaClosureEventId { get; set; }

    /// <summary>
    /// Productos asociados a esta venta.
    /// </summary>
    public ICollection<BzaSoldProduct> Products { get; set; } = [];

    /// <summary>
    /// Total de la venta, calculado en memoria a partir de los productos.
    /// NO se persiste en base de datos.
    /// </summary>
    [NotMapped]
    public decimal Total => Products?.Sum(p => p.Price) ?? 0m;
}
