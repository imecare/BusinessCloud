using BusinessCloud.Application.Payments.Dtos;
using MediatR;

namespace BusinessCloud.Application.Payments.Queries.GetAllSellers;

public record GetAllSellersQuery : IRequest<List<SellerDto>>;