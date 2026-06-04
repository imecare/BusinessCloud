using MediatR;

namespace BusinessCloud.Application.Payments.Commands.UpdateSeller;

public record UpdateSellerCommand(
    int Id,
    string Name,
    string LastName,
    string Phone
) : IRequest<bool>;
