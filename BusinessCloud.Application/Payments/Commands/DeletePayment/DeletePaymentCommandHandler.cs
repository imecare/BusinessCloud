using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Payments.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Commands.DeletePayment;

public class DeletePaymentCommandHandler : IRequestHandler<DeletePaymentCommand, bool>
{
    private readonly IPaymentsDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeletePaymentCommandHandler(IPaymentsDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<bool> Handle(DeletePaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await _db.Payments
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (payment is null)
            return false;

        // 1. Mover a tabla de auditoría de abonos eliminados
        var deletedPayment = new DeletedPayment
        {
            OriginalPaymentId = payment.Id,
            SaleId = payment.SaleId,
            Amount = payment.Amount,
            PaymentDate = payment.Date,
            PaymentMethod = payment.PaymentMethod,
            Reference = payment.Reference,
            PaymentTypeId = payment.PaymentTypeId,
            TenantId = payment.TenantId,
            OriginalCreatedAt = payment.CreatedAt,
            OriginalCreatedBy = payment.CreatedBy,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = _currentUser.UserId,
            DeletedReason = request.Reason
        };

        _db.DeletedPayments.Add(deletedPayment);

        // 2. Eliminar el pago original
        _db.Payments.Remove(payment);

        // 3. Recalcular IsPaid de la venta
        var sale = await _db.Sales
            .Include(s => s.Payment)
            .FirstOrDefaultAsync(s => s.Id == payment.SaleId, cancellationToken);

        if (sale is not null)
        {
            var totalPaid = sale.Payment
                .Where(p => p.Id != payment.Id)
                .Sum(p => p.Amount);

            sale.IsPaid = totalPaid >= sale.TotalAmount;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}