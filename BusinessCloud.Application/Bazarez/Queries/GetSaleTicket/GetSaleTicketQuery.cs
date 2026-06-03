using MediatR;

namespace BusinessCloud.Application.Bazares.Queries.GetSaleTicket;

public record GetSaleTicketQuery(int BzaSaleId) : IRequest<SaleTicketDto>;

public class SaleTicketDto
{
    public int SaleId { get; set; }
    public string? SaleDescription { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaymentDeadline { get; set; }

    // Cliente
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerAddress { get; set; }

    // Status
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;

    // Productos
    public List<SaleTicketProductDto> Products { get; set; } = new();

    // Totales
    public decimal Subtotal { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal PendingAmount { get; set; }
    public bool IsPaid { get; set; }

    // Pagos
    public List<SaleTicketPaymentDto> Payments { get; set; } = new();

    // Para QR
    public string? LabelCode { get; set; }
}

public class SaleTicketProductDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class SaleTicketPaymentDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public int PaymentStatus { get; set; }
    public string PaymentStatusName { get; set; } = string.Empty;
    public string? Reference { get; set; }
}
