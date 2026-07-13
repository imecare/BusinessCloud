using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Bazares.Entities;

public class BzaCollectorGroup : BaseAuditableEntity
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Día de la semana en que el grupo realiza la entrega.
    /// Valores según <see cref="DayOfWeek"/> (0=Domingo ... 6=Sábado). Null si no está definido.
    /// </summary>
    public int? DeliveryDay { get; set; }

    public bool IsActive { get; set; } = true;
    public ICollection<BzaCollector> Collectors { get; set; } = new List<BzaCollector>();
}
