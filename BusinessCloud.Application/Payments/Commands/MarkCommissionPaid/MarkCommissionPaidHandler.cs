using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Commands.MarkCommissionPaid;

public class MarkCommissionPaidHandler : IRequestHandler<MarkCommissionPaidCommand, MarkCommissionPaidResult>
{
    private readonly IPaymentsDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IMongoContext _mongoContext;

    public MarkCommissionPaidHandler(IPaymentsDbContext db, ICurrentUserService currentUser, IMongoContext mongoContext)
    {
        _db = db;
        _currentUser = currentUser;
        _mongoContext = mongoContext;
    }

    public async Task<MarkCommissionPaidResult> Handle(MarkCommissionPaidCommand request, CancellationToken cancellationToken)
    {
        var sale = await _db.Sales
            .FirstOrDefaultAsync(s => s.Id == request.SaleId, cancellationToken);

        if (sale is null)
            return new MarkCommissionPaidResult { Success = false, Message = "Venta no encontrada." };

        if (sale.CommissionAmount <= 0)
            return new MarkCommissionPaidResult { Success = false, Message = "Esta venta no tiene comisión asignada." };

        if (request.Paid)
        {
            // Regla: Solo pagar comisión si la venta está liquidada
            if (!sale.IsPaid)
                return new MarkCommissionPaidResult { Success = false, Message = "No se puede pagar comisión: la venta no ha sido liquidada por el cliente." };

            sale.IsCommissionPaid = true;
            sale.CommissionPaidAt = DateTime.UtcNow;
            sale.CommissionPaidByUserId = _currentUser.UserId;
            sale.CommissionPaymentNote = request.Note;
        }
        else
        {
            // Revertir pago de comisión
            sale.IsCommissionPaid = false;
            sale.CommissionPaidAt = null;
            sale.CommissionPaidByUserId = null;
            sale.CommissionPaymentNote = $"Revertido: {request.Note}";
        }

        await _db.SaveChangesAsync(cancellationToken);

        // Auditoría en MongoDB
        await _mongoContext.InsertAuditLogAsync(new
        {
            Event = request.Paid ? "CommissionMarkedPaid" : "CommissionReverted",
            SaleId = sale.Id,
            TenantId = sale.TenantId,
            PaidBy = _currentUser.UserId,
            Note = request.Note,
            Timestamp = DateTime.UtcNow
        }, cancellationToken);

        return new MarkCommissionPaidResult
        {
            Success = true,
            Message = request.Paid ? "Comisión marcada como pagada." : "Pago de comisión revertido."
        };
    }
}