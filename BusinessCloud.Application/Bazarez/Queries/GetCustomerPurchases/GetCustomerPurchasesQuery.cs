using MediatR;

namespace BusinessCloud.Application.Bazares.Queries.GetCustomerPurchases;

public record GetCustomerPurchasesQuery : IRequest<CustomerPurchasesDto>
{
    public int BzaCustomerId { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}

public class CustomerPurchasesDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public decimal TotalPurchases { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalPending { get; set; }
    public List<CustomerSaleDto> Sales { get; set; } = new();
}

public class CustomerSaleDto
{
    public int SaleId { get; set; }
    public string? SaleDescription { get; set; }
    public decimal SaleTotal { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime? PaymentDeadline { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<CustomerProductDto> Products { get; set; } = new();
}

public class CustomerProductDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
