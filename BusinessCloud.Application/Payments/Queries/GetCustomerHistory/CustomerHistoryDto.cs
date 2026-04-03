namespace BusinessCloud.Application.Payments.Queries.GetCustomerHistory;

public record CustomerHistoryDto(
    int SaleId,
    DateTime Date,
    decimal TotalAmount,      // Monto original de la venta
    decimal RemainingBalance, // Saldo actual pendiente
    string Status,            // "Pendiente", "Pagado"
    List<PaymentLineDto> Movements // <--- Nueva lista de abonos/movimientos
);

public record PaymentLineDto(
    int PaymentId,
    DateTime Date,
    decimal Amount,
    string Reference
);