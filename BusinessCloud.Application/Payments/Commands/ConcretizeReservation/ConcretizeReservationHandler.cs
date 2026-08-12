using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Payments.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Commands.ConcretizeReservation;

public class ConcretizeReservationHandler : IRequestHandler<ConcretizeReservationCommand, int?>
{
    private readonly IPaymentsDbContext _db;

    public ConcretizeReservationHandler(IPaymentsDbContext db) => _db = db;

    public async Task<int?> Handle(ConcretizeReservationCommand request, CancellationToken cancellationToken)
    {
        var reservation = await _db.Reservations
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (reservation is null) return null;

        var sale = new Sale
        {
            CustomerId = reservation.CustomerId,
            SellerId = reservation.SellerId,
            TotalAmount = reservation.TotalAmount,
            CostPrice = reservation.CostPrice,
            CommissionAmount = reservation.CommissionAmount,
            ProductDescription = reservation.ProductDescription,
            IsCommissionPaid = false,
            IsPaid = false,
            Date = DateTime.UtcNow
        };

        // Movimiento inicial de la venta (mismo patrón que CreateSaleHandler).
        var initialMovement = new Payment
        {
            Amount = reservation.TotalAmount,
            PaymentDate = sale.Date,
            Date = DateTime.UtcNow,
            PaymentTypeId = 1,
            Reference = "Registro inicial de venta (apartado concretado)"
        };

        sale.Payment = new List<Payment> { initialMovement };

        _db.Sales.Add(sale);
        _db.Reservations.Remove(reservation);

        await _db.SaveChangesAsync(cancellationToken);

        return sale.Id;
    }
}
