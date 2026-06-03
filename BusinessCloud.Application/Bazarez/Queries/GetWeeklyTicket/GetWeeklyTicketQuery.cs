using MediatR;

namespace BusinessCloud.Application.Bazares.Queries.GetWeeklyTicket;

/// <summary>
/// Query para obtener el ticket consolidado de la semana para un cliente.
/// Agrupa todos los Eventos de Venta activos en la semana con sus desgloses y un Gran Total Combinado.
/// </summary>
public record GetWeeklyTicketQuery(int BzaCustomerId) : IRequest<WeeklyTicketDto>;

public class WeeklyTicketDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string CollectorName { get; set; } = string.Empty;
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public DateTime? EarliestPaymentDeadline { get; set; }

    /// <summary>
    /// Eventos de Venta donde el cliente tiene productos esta semana.
    /// </summary>
    public List<WeeklyEventItemDto> Events { get; set; } = [];

    public decimal GrandTotal { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal RemainingBalance { get; set; }
}

public class WeeklyEventItemDto
{
    public int SaleEventId { get; set; }
    public string EventDescription { get; set; } = string.Empty;
    public DateTime? PaymentDeadline { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public List<WeeklyProductDto> Products { get; set; } = [];
    public decimal Subtotal { get; set; }
    public decimal Paid { get; set; }
    public decimal Pending { get; set; }
}

public class WeeklyProductDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
