using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Commands.DeletePayment;

public class DeletePaymentCommandHandler : IRequestHandler<DeletePaymentCommand, bool>
{
    private readonly IPaymentsDbContext _db;

    public DeletePaymentCommandHandler(IPaymentsDbContext db) => _db = db;

    public async Task<bool> Handle(DeletePaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await _db.Payments
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (payment is null)
            return false;

        _db.Payments.Remove(payment);
        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }
}