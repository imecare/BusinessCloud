using MediatR;

namespace BusinessCloud.Application.Bazares.Queries.GetWeeklyTicket;

public record GetWeeklyTicketQuery(int BzaCustomerId) : IRequest<WeeklyTicketDto>;

public class WeeklyTicketDto
{
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string CollectorName { get; set; } = string.Empty;
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public DateTime? PaymentDeadline { get; set; }
    public List<WeeklyTicketItemDto> Items { get; set; } = new();
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingBalance { get; set; }
}

public class WeeklyTicketItemDto
{
    public int SaleId { get; set; }
    public string? Description { get; set; }
    public List<string> Products { get; set; } = new();
    public decimal Total { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
}
