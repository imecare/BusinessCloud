using MediatR;

namespace BusinessCloud.Application.Bazares.Queries.GetCustomerSalesHistory;

/// <summary>
/// Query para obtener el historial completo del cliente agrupado por Evento de Venta (BzaSaleId).
/// </summary>
public record GetCustomerSalesHistoryQuery(int BzaCustomerId) : IRequest<CustomerSalesHistoryDto>;

public class CustomerSalesHistoryDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;

    // Totales generales
    public decimal TotalPurchases { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalPending { get; set; }
    public int TotalEvents { get; set; }
    public int PaidEvents { get; set; }
    public int PendingEvents { get; set; }

    // Eventos de Venta donde el cliente tiene compras
    public List<EventHistoryGroupDto> Events { get; set; } = [];
}

public class EventHistoryGroupDto
{
    public int SaleEventId { get; set; }
    public string EventDescription { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? PaymentDeadline { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public int EventStatus { get; set; }
    public string EventStatusName { get; set; } = string.Empty;
    public bool IsCustomerPaid { get; set; }

    // Productos del cliente en este evento
    public List<EventHistoryProductDto> Products { get; set; } = [];

    // Totales del cliente en este evento
    public decimal Subtotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PendingAmount { get; set; }

    // Pagos del cliente en este evento
    public List<EventHistoryPaymentDto> Payments { get; set; } = [];
}

public class EventHistoryProductDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class EventHistoryPaymentDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public int PaymentStatus { get; set; }
    public string PaymentStatusName { get; set; } = string.Empty;
}
