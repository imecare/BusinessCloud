using MediatR;

namespace BusinessCloud.Application.Payments.Commands.UpdatePayment;

public record UpdatePaymentCommand(
    int Id,
    decimal Amount,
    string PaymentMethod,
    string? Reference
) : IRequest<bool>;
