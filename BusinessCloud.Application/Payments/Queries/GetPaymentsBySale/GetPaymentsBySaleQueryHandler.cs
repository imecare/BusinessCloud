using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Payments.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Queries.GetPaymentsBySale;

public class GetPaymentsBySaleQueryHandler : IRequestHandler<GetPaymentsBySaleQuery, List<PaymentDto>>
{
    private readonly IPaymentsDbContext _db;

    public GetPaymentsBySaleQueryHandler(IPaymentsDbContext db) => _db = db;

    public async Task<List<PaymentDto>> Handle(GetPaymentsBySaleQuery request, CancellationToken cancellationToken)
    {
        return await _db.Payments
            .AsNoTracking()
            .Where(p => p.SaleId == request.SaleId)
            .OrderBy(p => p.PaymentDate)
            .Select(p => new PaymentDto
            {
                Id = p.Id,
                SaleId = p.SaleId,
                Amount = p.Amount,
                PaymentDate = p.PaymentDate,
                Date = p.Date,
                PaymentMethod = p.PaymentMethod,
                Reference = p.Reference,
                PaymentTypeId = p.PaymentTypeId
            })
            .ToListAsync(cancellationToken);
    }
}