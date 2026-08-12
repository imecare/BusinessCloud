using MediatR;

namespace BusinessCloud.Application.Payments.Commands.UpdateExpense;

public record UpdateExpenseCommand(
    int Id,
    DateTime Date,
    string Description,
    decimal Cost,
    string PaymentType,
    int? Months
) : IRequest<bool>;
