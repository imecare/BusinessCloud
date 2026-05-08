using MediatR;

namespace BusinessCloud.Application.Bazares.Queries.GetCustomerPortal;

public record GetCustomerPortalQuery(string PortalToken) : IRequest<CustomerPortalDto>;

public class CustomerPortalDto
{
    public string CustomerName { get; set; } = string.Empty;
    public string CollectorName { get; set; } = string.Empty;
    public string? CollectorGroup { get; set; }
    public List<CustomerPortalSaleDto> ActiveSales { get; set; } = new();
    public List<CustomerPortalSaleDto> History { get; set; } = new();
    public decimal TotalPending { get; set; }
    public string? BankInfo { get; set; }
}

public class CustomerPortalSaleDto
{
    public int SaleId { get; set; }
    public string? Description { get; set; }
    public List<string> Products { get; set; } = new();
    public decimal Total { get; set; }
    public decimal Paid { get; set; }
    public decimal Remaining { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime? PaymentDeadline { get; set; }
    public DateTime CreatedAt { get; set; }
}
