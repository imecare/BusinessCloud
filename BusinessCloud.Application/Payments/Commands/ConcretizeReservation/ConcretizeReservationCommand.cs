using MediatR;

namespace BusinessCloud.Application.Payments.Commands.ConcretizeReservation;

/// <summary>
/// Concreta un apartado: crea la venta a partir de la reserva y elimina el apartado.
/// Devuelve el Id de la nueva venta, o null si el apartado no existe.
/// </summary>
public record ConcretizeReservationCommand(int Id) : IRequest<int?>;
