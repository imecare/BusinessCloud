using MediatR;

namespace BusinessCloud.Application.Payments.Commands.UpdateCustomer;

public record UpdateCustomerCommand(
    int Id,
    string Name,
    string LastName,
    string RFC,
    string Phone,
    int SellerId
) : IRequest<bool>;
