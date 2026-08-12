using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Payments.Entities;
using MediatR;

namespace BusinessCloud.Application.Payments.Commands.CreateExpense
{
    public class CreateExpenseHandler : IRequestHandler<CreateExpenseCommand, int>
    {
        private readonly IPaymentsDbContext _db;

        public CreateExpenseHandler(IPaymentsDbContext db) => _db = db;

        public async Task<int> Handle(CreateExpenseCommand request, CancellationToken cancellationToken)
        {
            var isInstallments = request.PaymentType == ExpensePaymentTypes.Installments;

            var expense = new PayExpense
            {
                Date = request.Date,
                Description = request.Description,
                Cost = request.Cost,
                PaymentType = isInstallments ? ExpensePaymentTypes.Installments : ExpensePaymentTypes.Cash,
                Months = isInstallments ? request.Months : null
            };

            _db.Expenses.Add(expense);
            await _db.SaveChangesAsync(cancellationToken);

            return expense.Id;
        }
    }
}
