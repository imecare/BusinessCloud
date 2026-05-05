using BusinessCloud.Application.Payments.Dtos;
using MediatR;

namespace BusinessCloud.Application.Payments.Queries.GetMySales;

public record GetMySalesQuery : IRequest<List<CommissionistSaleDto>>;