using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Commands.MarkExpenseReceived;

public class MarkExpenseReceivedHandler : IRequestHandler<MarkExpenseReceivedCommand, bool>
{
    private readonly IPaymentsDbContext _db;

    public MarkExpenseReceivedHandler(IPaymentsDbContext db) => _db = db;

    public async Task<bool> Handle(MarkExpenseReceivedCommand request, CancellationToken cancellationToken)
    {
        var expense = await _db.Expenses
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        if (expense is null) return false;

        expense.IsReceived = request.Received;
        expense.ReceivedAt = request.Received ? DateTime.UtcNow : null;

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
