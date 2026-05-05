using BusinessCloud.Application.Payments.Dtos;
using MediatR;

namespace BusinessCloud.Application.Payments.Queries.GetMyCustomers;

public record GetMyCustomersQuery : IRequest<List<CustomerDto>>;