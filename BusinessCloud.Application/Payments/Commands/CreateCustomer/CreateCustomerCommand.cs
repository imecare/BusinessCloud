using BusinessCloud.Domain.Payments.Entities;
using MediatR;

namespace BusinessCloud.Application.Payments.Commands.CreateCustomer
{
    public record CreateCustomerCommand(
        string Name,
        string LastName,
        string RFC,
        string Phone,
        int SellerId 
    ) : IRequest<int>;
}
