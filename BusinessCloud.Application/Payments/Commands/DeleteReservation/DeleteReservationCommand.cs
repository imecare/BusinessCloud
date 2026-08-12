using MediatR;

namespace BusinessCloud.Application.Payments.Commands.DeleteReservation;

public record DeleteReservationCommand(int Id) : IRequest<bool>;
