using BusinessCloud.Application.Payments.Dtos;
using MediatR;

namespace BusinessCloud.Application.Payments.Queries.GetMyReservations;

public record GetMyReservationsQuery : IRequest<List<ReservationDto>>;
