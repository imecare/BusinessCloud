using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Payments.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Queries.GetAllPayments;

public class GetAllPaymentsQueryHandler : IRequestHandler<GetAllPaymentsQuery, List<PaymentDto>>
{
    private readonly IPaymentsDbContext _db;

    public GetAllPaymentsQueryHandler(IPaymentsDbContext db) => _db = db;

    public async Task<List<PaymentDto>> Handle(GetAllPaymentsQuery request, CancellationToken cancellationToken)
    {
        return await _db.Payments
            .AsNoTracking()
            .Select(p => new PaymentDto
            {
                Id = p.Id,
                SaleId = p.SaleId,
                Amount = p.Amount,
                PaymentDate = p.PaymentDate,
                Date = p.PaymentDate,
                PaymentMethod = p.PaymentMethod,
                Reference = p.Reference,
                PaymentTypeId = p.PaymentTypeId
            })
            .ToListAsync(cancellationToken);
    }
}