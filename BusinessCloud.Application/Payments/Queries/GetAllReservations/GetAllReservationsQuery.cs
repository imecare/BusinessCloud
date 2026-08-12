using BusinessCloud.Application.Payments.Dtos;
using MediatR;

namespace BusinessCloud.Application.Payments.Queries.GetAllReservations;

public record GetAllReservationsQuery : IRequest<List<ReservationDto>>;
