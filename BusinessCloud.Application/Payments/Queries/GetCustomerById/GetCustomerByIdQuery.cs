using MediatR;
using BusinessCloud.Application.Payments.Dtos;


namespace BusinessCloud.Application.Payments.Queries.GetCustomerById
{
    public record GetCustomerByIdQuery(int Id) : IRequest<CustomerDto?>;
}
