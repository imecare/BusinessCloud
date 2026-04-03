using BusinessCloud.Application.Payments.Queries.GetCustomerHistory;

namespace BusinessCloud.Application.Payments.Commands.RegisterPayment;

public record PaymentReceiptDto(
    string Folio,
    string CustomerName,
    decimal AmountPaid,
    decimal NewBalance,
    DateTime Date,
    List<PaymentLineDto> LastMovements
);