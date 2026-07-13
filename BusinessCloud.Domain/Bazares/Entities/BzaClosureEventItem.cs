using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Bazares.Entities;

/// <summary>
/// Vínculo entre un Evento de Cierre de Venta y un Evento de Venta (BzaEvent) que abarca.
/// </summary>
public class BzaClosureEventItem : BaseAuditableEntity
{
    public int Id { get; set; }

    public int BzaClosureEventId { get; set; }
    public BzaClosureEvent ClosureEvent { get; set; } = null!;

    public int BzaEventId { get; set; }
    public BzaEvent Event { get; set; } = null!;
}
