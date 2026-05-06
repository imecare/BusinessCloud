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

        // Recalcular IsPaid de la venta
        var sale = await _db.Sales
            .Include(s => s.Payment)
            .FirstOrDefaultAsync(s => s.Id == payment.SaleId, cancellationToken);

        if (sale is not null)
        {
            var totalPaid = sale.Payment.Sum(p => p.Id == payment.Id ? request.Amount : p.Amount);
            sale.IsPaid = totalPaid >= sale.TotalAmount;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
