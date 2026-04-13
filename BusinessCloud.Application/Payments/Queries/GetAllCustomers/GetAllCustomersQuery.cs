using BusinessCloud.Application.Payments.Dtos;
using MediatR;

namespace BusinessCloud.Application.Payments.Queries.GetAllCustomers;

public class GetAllCustomersQuery : IRequest<List<CustomerDto>>
{
}   