using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Commands.DeleteExpense;

public class DeleteExpenseHandler : IRequestHandler<DeleteExpenseCommand, bool>
{
    private readonly IPaymentsDbContext _db;

    public DeleteExpenseHandler(IPaymentsDbContext db) => _db = db;

    public async Task<bool> Handle(DeleteExpenseCommand request, CancellationToken cancellationToken)
    {
        var expense = await _db.Expenses
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        if (expense is null) return false;

        _db.Expenses.Remove(expense);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
