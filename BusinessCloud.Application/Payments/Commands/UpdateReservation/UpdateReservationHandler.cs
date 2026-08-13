using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Commands.UpdateReservation;

public class UpdateReservationHandler : IRequestHandler<UpdateReservationCommand, bool>
{
    private readonly IPaymentsDbContext _db;

    public UpdateReservationHandler(IPaymentsDbContext db) => _db = db;

    public async Task<bool> Handle(UpdateReservationCommand request, CancellationToken cancellationToken)
    {
        var reservation = await _db.Reservations
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (reservation is null) return false;

        reservation.CustomerId = request.CustomerId;
        reservation.SellerId = request.SellerId;
        reservation.TotalAmount = request.TotalAmount;
        reservation.CostPrice = request.CostPrice;
        reservation.CommissionAmount = request.CommissionAmount;
        reservation.ProductDescription = request.ProductDescription;
        reservation.Date = request.Date;

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
