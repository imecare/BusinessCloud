using MediatR;

namespace BusinessCloud.Application.Payments.Queries.GetCustomerDebts;

public sealed record GetCustomerDebtsQuery : IRequest<List<CustomerDebtDto>>;