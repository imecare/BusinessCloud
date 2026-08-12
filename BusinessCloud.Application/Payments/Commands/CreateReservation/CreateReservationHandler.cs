using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Payments.Entities;
using MediatR;

namespace BusinessCloud.Application.Payments.Commands.CreateReservation;

public class CreateReservationHandler : IRequestHandler<CreateReservationCommand, int>
{
    private readonly IPaymentsDbContext _db;

    public CreateReservationHandler(IPaymentsDbContext db) => _db = db;

    public async Task<int> Handle(CreateReservationCommand request, CancellationToken cancellationToken)
    {
        var reservation = new SaleReservation
        {
            CustomerId = request.CustomerId,
            SellerId = request.SellerId,
            TotalAmount = request.TotalAmount,
            CostPrice = request.CostPrice,
            CommissionAmount = request.CommissionAmount,
            ProductDescription = request.ProductDescription,
            Date = request.Date
        };

        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync(cancellationToken);

        return reservation.Id;
    }
}
