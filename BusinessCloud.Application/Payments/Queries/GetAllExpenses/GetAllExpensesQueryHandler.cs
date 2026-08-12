using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Payments.Dtos;
using BusinessCloud.Domain.Payments.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Queries.GetAllExpenses;

public class GetAllExpensesQueryHandler : IRequestHandler<GetAllExpensesQuery, List<ExpenseDto>>
{
    private readonly IPaymentsDbContext _db;

    public GetAllExpensesQueryHandler(IPaymentsDbContext db) => _db = db;

    public async Task<List<ExpenseDto>> Handle(GetAllExpensesQuery request, CancellationToken cancellationToken)
    {
        return await _db.Expenses
            .AsNoTracking()
            .OrderByDescending(e => e.Date)
            .Select(e => new ExpenseDto
            {
                Id = e.Id,
                Date = e.Date,
                Description = e.Description,
                Cost = e.Cost,
                PaymentType = e.PaymentType,
                Months = e.Months,
                MonthlyAmount = e.PaymentType == ExpensePaymentTypes.Installments && e.Months != null && e.Months > 0
                    ? e.Cost / e.Months.Value
                    : (decimal?)null
            })
            .ToListAsync(cancellationToken);
    }
}
