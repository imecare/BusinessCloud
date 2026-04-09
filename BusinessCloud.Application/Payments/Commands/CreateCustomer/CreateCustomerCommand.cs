using MediatR;

namespace BusinessCloud.Application.Payments.Commands.CreateCustomer
{
    public record CreateSellerCommand(
        string Name,
        string LastName,
        string RFC,
        string Phone,
        int SellerId
    ) : IRequest<int>;
}
