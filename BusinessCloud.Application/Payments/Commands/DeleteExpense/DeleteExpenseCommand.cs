using MediatR;

namespace BusinessCloud.Application.Payments.Commands.DeleteExpense;

public record DeleteExpenseCommand(int Id) : IRequest<bool>;
