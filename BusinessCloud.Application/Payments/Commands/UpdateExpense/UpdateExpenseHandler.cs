using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Payments.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Commands.UpdateExpense;

public class UpdateExpenseHandler : IRequestHandler<UpdateExpenseCommand, bool>
{
    private readonly IPaymentsDbContext _db;

    public UpdateExpenseHandler(IPaymentsDbContext db) => _db = db;

    public async Task<bool> Handle(UpdateExpenseCommand request, CancellationToken cancellationToken)
    {
        var expense = await _db.Expenses
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        if (expense is null) return false;

        var isInstallments = request.PaymentType == ExpensePaymentTypes.Installments;

        expense.Date = request.Date;
        expense.Description = request.Description;
        expense.Cost = request.Cost;
        expense.PaymentType = isInstallments ? ExpensePaymentTypes.Installments : ExpensePaymentTypes.Cash;
        expense.Months = isInstallments ? request.Months : null;

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
