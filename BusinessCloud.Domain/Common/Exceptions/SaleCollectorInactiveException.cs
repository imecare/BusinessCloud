namespace BusinessCloud.Domain.Common.Exceptions;

/// <summary>
/// Se lanza al intentar registrar una venta a un cliente cuyo recolector (o el grupo
/// del recolector) está inactivo. Lleva la información necesaria para que el frontend
/// ofrezca activar el recolector/grupo y reintentar.
/// </summary>
public class SaleCollectorInactiveException : Exception
{
    /// <summary>Código: "COLLECTOR_INACTIVE" o "COLLECTOR_GROUP_INACTIVE".</summary>
    public string Code { get; }
    public int? CollectorId { get; }
    public string? CollectorName { get; }
    public bool CollectorInactive { get; }
    public int? GroupId { get; }
    public string? GroupDescription { get; }
    public bool GroupInactive { get; }

    public SaleCollectorInactiveException(
        string message,
        string code,
        int? collectorId,
        string? collectorName,
        bool collectorInactive,
        int? groupId,
        string? groupDescription,
        bool groupInactive) : base(message)
    {
        Code = code;
        CollectorId = collectorId;
        CollectorName = collectorName;
        CollectorInactive = collectorInactive;
        GroupId = groupId;
        GroupDescription = groupDescription;
        GroupInactive = groupInactive;
    }
}
