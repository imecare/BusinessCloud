using MediatR;

namespace BusinessCloud.Application.Payments.Commands.CreateReservation;

public record CreateReservationCommand(
    int CustomerId,
    int? SellerId,
    decimal TotalAmount,
    decimal CostPrice,
    decimal CommissionAmount,
    string ProductDescription,
    DateTime Date
) : IRequest<int>;
