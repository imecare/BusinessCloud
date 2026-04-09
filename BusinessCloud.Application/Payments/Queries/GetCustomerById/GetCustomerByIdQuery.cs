using MediatR;
using BusinessCloud.Application.Payments.Dtos;


namespace BusinessCloud.Application.Payments.Queries.GetCustomerById
{
    public record GetSellerByIdQuery(int Id) : IRequest<CustomerDto?>;
}
