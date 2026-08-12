using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Commands.DeleteReservation;

public class DeleteReservationHandler : IRequestHandler<DeleteReservationCommand, bool>
{
    private readonly IPaymentsDbContext _db;

    public DeleteReservationHandler(IPaymentsDbContext db) => _db = db;

    public async Task<bool> Handle(DeleteReservationCommand request, CancellationToken cancellationToken)
    {
        var reservation = await _db.Reservations
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (reservation is null) return false;

        _db.Reservations.Remove(reservation);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
