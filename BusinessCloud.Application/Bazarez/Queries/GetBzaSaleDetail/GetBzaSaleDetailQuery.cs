using MediatR;

namespace BusinessCloud.Application.Bazares.Queries.GetBzaSaleDetail;

public record GetBzaSaleDetailQuery(int Id) : IRequest<BzaSaleDetailDto>;

/// <summary>
/// DTO con métricas consolidadas del Evento de Venta.
/// </summary>
public class BzaSaleMetricsDto
{
    public decimal TotalRevenue { get; set; }
    public int ProductsCount { get; set; }
    public int UniqueCustomersCount { get; set; }
    public int UniqueCustomers { get; set; }
    public int TotalProducts { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal PendingAmount { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal TotalPending { get; set; }
    public decimal CollectionPercentage { get; set; }
}

/// <summary>
/// DTO con el detalle de un Evento de Venta.
/// </summary>
public class BzaSaleDetailDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime? PaymentDeadline { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;

    /// <summary>
    /// Id del Evento de Cierre (envío de totales) al que pertenecen las ventas de este
    /// evento, si ya se generó. Null si todavía no se ha hecho el envío de totales.
    /// </summary>
    public int? ClosureEventId { get; set; }

    /// <summary>
    /// M�tricas del evento para dashboard de detalle.
    /// </summary>
    public BzaSaleMetricsDto Metrics { get; set; } = new();

    /// <summary>
    /// Total de ingresos del evento (suma de precios de todos los productos).
    /// </summary>
    public decimal TotalRevenue { get; set; }

    /// <summary>
    /// Cantidad total de productos en el evento.
    /// </summary>
    public int ProductsCount { get; set; }

    /// <summary>
    /// Cantidad de clientes únicos con compras en este evento.
    /// </summary>
    public int UniqueCustomersCount { get; set; }

    /// <summary>
    /// Total de pagos aprobados.
    /// </summary>
    public decimal TotalPaid { get; set; }

    /// <summary>
    /// Saldo pendiente del evento.
    /// </summary>
    public decimal PendingAmount { get; set; }

    /// <summary>
    /// Historial de auditoría del evento (desde MongoDB).
    /// </summary>
    public List<BzaSaleAuditDto> AuditHistory { get; set; } = [];
}

public class BzaSaleAuditDto
{
    public string Event { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Details { get; set; } = string.Empty;
}
