using BusinessCloud.Application.Payments.Dtos;
using MediatR;

namespace BusinessCloud.Application.Payments.Queries.GetAllSales;

public record GetAllSalesQuery : IRequest<List<AdminSaleDto>>;
