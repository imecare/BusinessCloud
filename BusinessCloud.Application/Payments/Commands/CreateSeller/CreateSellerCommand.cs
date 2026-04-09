using MediatR;

namespace BusinessCloud.Application.Payments.Commands.CreateSeller
{
    public record CreateSellerCommand(
        string Name,
        string LastName,
        string Phone
    ) : IRequest<int>;
}
