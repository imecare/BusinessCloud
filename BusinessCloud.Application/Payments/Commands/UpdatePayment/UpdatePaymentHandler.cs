using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Commands.UpdatePayment;

public class UpdatePaymentHandler : IRequestHandler<UpdatePaymentCommand, bool>
{
    private readonly IPaymentsDbContext _db;

    public UpdatePaymentHandler(IPaymentsDbContext db) => _db = db;

    public async Task<bool> Handle(UpdatePaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await _db.Payments
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (payment is null) return false;

        payment.Amount = request.Amount;
        payment.PaymentMethod = request.PaymentMethod;
        payment.Reference = request.Reference;
        if (request.PaymentDate.HasValue)
            payment.PaymentDate = request.PaymentDate.Value;

        // Recalcular IsPaid de la venta
        var sale = await _db.Sales
            .FirstOrDefaultAsync(s => s.Id == payment.SaleId, cancellationToken);

        if (sale is not null)
        {
            // Sumar los demás abonos desde la BD y agregar el monto actualizado de este abono
            var otherPaid = await _db.Payments
                .Where(p => p.SaleId == payment.SaleId && p.Id != payment.Id)
                .SumAsync(p => p.Amount, cancellationToken);

            sale.IsPaid = (otherPaid + request.Amount) >= sale.TotalAmount;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
