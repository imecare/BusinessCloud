using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Bazares.Entities;

/// <summary>
/// Fecha de entrega asignada a un grupo de recolección dentro de un Evento de Cierre de Venta.
/// Se crea un registro por cada grupo que tiene compras en el conjunto de ventas del cierre.
/// La fecha por defecto se sugiere según el día de entrega configurado en el grupo (DeliveryDay).
/// </summary>
public class BzaClosureGroupDelivery : BaseAuditableEntity
{
    public int Id { get; set; }

    public int BzaClosureEventId { get; set; }
    public BzaClosureEvent ClosureEvent { get; set; } = null!;

    public int BzaCollectorGroupId { get; set; }
    public BzaCollectorGroup CollectorGroup { get; set; } = null!;

    /// <summary>Fecha de entrega asignada para este grupo en este cierre.</summary>
    public DateTime DeliveryDate { get; set; }
}
