using MediatR;

namespace BusinessCloud.Application.Payments.Commands.UpdateReservation;

public record UpdateReservationCommand(
    int Id,
    int CustomerId,
    int? SellerId,
    decimal TotalAmount,
    decimal CostPrice,
    decimal CommissionAmount,
    string ProductDescription,
    DateTime Date
) : IRequest<bool>;
