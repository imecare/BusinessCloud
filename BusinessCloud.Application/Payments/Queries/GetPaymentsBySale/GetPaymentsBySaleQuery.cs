using BusinessCloud.Application.Payments.Dtos;
using MediatR;

namespace BusinessCloud.Application.Payments.Queries.GetPaymentsBySale;

public record GetPaymentsBySaleQuery(int SaleId) : IRequest<List<PaymentDto>>;