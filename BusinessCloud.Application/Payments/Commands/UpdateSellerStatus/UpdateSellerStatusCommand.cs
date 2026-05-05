using MediatR;

namespace BusinessCloud.Application.Payments.Commands.UpdateSellerStatus;

public record UpdateSellerStatusCommand(int Id, int StatusId) : IRequest<bool>;