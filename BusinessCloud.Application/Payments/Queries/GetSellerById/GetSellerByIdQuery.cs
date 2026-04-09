using MediatR;
using BusinessCloud.Application.Payments.Dtos;


namespace BusinessCloud.Application.Payments.Queries.GetSellerById
{
    public record GetSellerByIdQuery(int Id) : IRequest<SellerDto?>;
}
