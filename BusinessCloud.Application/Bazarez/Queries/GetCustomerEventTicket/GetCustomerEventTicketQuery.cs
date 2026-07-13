using MediatR;

namespace BusinessCloud.Application.Bazares.Queries.GetCustomerEventTicket;

/// <summary>
/// Query para obtener el ticket de un cliente en un Evento de Venta específico.
/// Detalla qué productos compró en ESE en vivo/catálogo, sus abonos y saldo restante.
/// </summary>
public record GetCustomerEventTicketQuery(int CustomerId, int SaleId) : IRequest<CustomerEventTicketDto>;

/// <summary>
/// DTO del ticket del cliente para un Evento de Venta específico.
/// </summary>
public class CustomerEventTicketDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public int SaleEventId { get; set; }
    public string EventDescription { get; set; } = string.Empty;
    public DateTime? PaymentDeadline { get; set; }
    public List<TicketProductDto> Products { get; set; } = [];
    public decimal Subtotal { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal PendingAmount { get; set; }
    public bool IsPaid { get; set; }
    public List<TicketPaymentDto> Payments { get; set; } = [];
}

public class TicketProductDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class TicketPaymentDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public int PaymentStatus { get; set; }
    public string PaymentStatusName { get; set; } = string.Empty;
}
