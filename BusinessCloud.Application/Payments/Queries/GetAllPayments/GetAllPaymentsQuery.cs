using BusinessCloud.Application.Payments.Dtos;
using MediatR;

namespace BusinessCloud.Application.Payments.Queries.GetAllPayments;

public class GetAllPaymentsQuery : IRequest<List<PaymentDto>>
{
}