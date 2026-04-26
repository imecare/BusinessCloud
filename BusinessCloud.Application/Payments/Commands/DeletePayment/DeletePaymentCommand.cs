using MediatR;

namespace BusinessCloud.Application.Payments.Commands.DeletePayment;

public record DeletePaymentCommand(int Id) : IRequest<bool>;