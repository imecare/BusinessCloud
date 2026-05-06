using MediatR;

namespace BusinessCloud.Application.Payments.Commands.DeletePayment;

public record DeletePaymentCommand(int Id, string? Reason = null) : IRequest<bool>;