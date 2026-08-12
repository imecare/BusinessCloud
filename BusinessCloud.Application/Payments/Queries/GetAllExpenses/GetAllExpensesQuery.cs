using BusinessCloud.Application.Payments.Dtos;
using MediatR;

namespace BusinessCloud.Application.Payments.Queries.GetAllExpenses;

public record GetAllExpensesQuery : IRequest<List<ExpenseDto>>;
