using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.DeleteBzaDelivery;

public record DeleteBzaDeliveryCommand(int Id) : IRequest<DeleteBzaDeliveryResult>;

public record DeleteBzaDeliveryResult(bool Success, string Message);
