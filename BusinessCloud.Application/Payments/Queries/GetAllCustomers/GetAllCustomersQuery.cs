using BusinessCloud.Application.Payments.Dtos;
using MediatR;

namespace BusinessCloud.Application.Payments.Queries.GetAllCustomers;

public record GetAllCustomersQuery : IRequest<IEnumerable<CustomerDto>>;