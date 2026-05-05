namespace BusinessCloud.Application.Payments.Queries.GetCustomerHistory;

public record CustomerHistoryDto(
    int SaleId,
    DateTime Date,
    string ProductDescription,
    decimal TotalAmount,
    decimal RemainingBalance,
    string Status,
    List<PaymentLineDto> Movements
);

public record PaymentLineDto(
    int PaymentId,
    DateTime Date,
    decimal Amount,
    string Reference
);