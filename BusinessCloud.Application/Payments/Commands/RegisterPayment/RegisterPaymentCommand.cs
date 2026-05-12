using MediatR;

namespace BusinessCloud.Application.Payments.Commands.RegisterPayment;

// Según el modelo de datos: N:1 con Sale [cite: 23]
public record RegisterPaymentCommand(
    int SaleId,
    decimal Amount,
    string Reference, // Ej: "Efectivo", "Transferencia 1234"
    DateTime PaymentDate // Fecha del abono enviada desde el front
) : IRequest<PaymentReceiptDto>;