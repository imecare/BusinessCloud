using BusinessCloud.Application.Payments.Dtos;
using MediatR;

namespace BusinessCloud.Application.Payments.Queries.GetActiveSellers;

public record GetActiveSellersQuery : IRequest<List<SellerDto>>;