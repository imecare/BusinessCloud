using MediatR;

namespace BusinessCloud.Application.Bazares.Queries.GetBzaSaleDetail;

public record GetBzaSaleDetailQuery(int Id) : IRequest<BzaSaleDetailDto>;

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