using BusinessCloud.Application.Payments.Dtos;
using MediatR;

namespace BusinessCloud.Application.Payments.Queries.GetCustomerById;

public class GetCustomerByIdQuery : IRequest<CustomerDto?>
{
    public int Id { get; set; }

    public GetCustomerByIdQuery(int id)
    {
        Id = id;
    }
}   