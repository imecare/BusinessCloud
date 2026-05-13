using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Payments.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Commands.DeleteSale;

public class DeleteSaleCommandHandler : IRequestHandler<DeleteSaleCommand, bool>
{
    private readonly IPaymentsDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteSaleCommandHandler(IPaymentsDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<bool> Handle(DeleteSaleCommand request, CancellationToken cancellationToken)
    {
        var sale = await _db.Sales
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (sale is null)
            return false;

        // Solo permitir eliminar si no tiene abonos (PaymentTypeId == 2)
        var hasPayments = await _db.Payments
            .AnyAsync(p => p.SaleId == sale.Id && p.PaymentTypeId == 2, cancellationToken);

        if (hasPayments)
            throw new InvalidOperationException("No se puede eliminar una venta que tiene abonos registrados. Elimine los abonos primero.");

        // 1. Mover a tabla de auditoría de ventas eliminadas
        var deletedSale = new DeletedSale
        {
            OriginalSaleId = sale.Id,
            CustomerId = sale.CustomerId,
            SellerId = sale.SellerId,
            TotalAmount = sale.TotalAmount,
            CostPrice = sale.CostPrice,
            CommissionAmount = sale.CommissionAmount,
            ProductDescription = sale.ProductDescription,
            IsCommissionPaid = sale.IsCommissionPaid,
            IsPaid = sale.IsPaid,
            Date = sale.Date,
            TenantId = sale.TenantId,
            OriginalCreatedAt = sale.CreatedAt,
            OriginalCreatedBy = sale.CreatedBy,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = _currentUser.UserId,
            DeletedReason = request.Reason
        };

        _db.DeletedSales.Add(deletedSale);

        // 2. Eliminar pagos tipo 1 (cargo inicial) asociados
        var initialCharges = await _db.Payments
            .Where(p => p.SaleId == sale.Id)
            .ToListAsync(cancellationToken);

        _db.Payments.RemoveRange(initialCharges);

        // 3. Eliminar la venta
        _db.Sales.Remove(sale);

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
