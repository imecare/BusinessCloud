using MediatR;

namespace BusinessCloud.Application.Payments.Commands.CreateExpense
{
    public record CreateExpenseCommand(
        DateTime Date,
        string Description,
        decimal Cost,
        string PaymentType,
        int? Months
    ) : IRequest<int>;
}
