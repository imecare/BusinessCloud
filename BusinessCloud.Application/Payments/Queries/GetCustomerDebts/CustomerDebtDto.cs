namespace BusinessCloud.Application.Payments.Queries.GetCustomerDebts;

public sealed record CustomerDebtDto(
    int CustomerId,
    string CustomerName,
    int? SellerId,
    string SellerName,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal DebtAmount,
    int PendingSalesCount,
    DateTime? LastPaymentDate);