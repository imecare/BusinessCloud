using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Payments.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Queries.GetMySales;

public class GetMySalesQueryHandler : IRequestHandler<GetMySalesQuery, List<CommissionistSaleDto>>
{
    private readonly IPaymentsDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMySalesQueryHandler(IPaymentsDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<CommissionistSaleDto>> Handle(GetMySalesQuery request, CancellationToken cancellationToken)
    {
        var sellerId = _currentUser.SellerId
            ?? throw new UnauthorizedAccessException("No se pudo determinar el SellerId del token.");

        return await _db.Sales
            .AsNoTracking()
            .Where(s => s.SellerId == sellerId)
            .Include(s => s.Customer)
            .Include(s => s.Payment)
            .OrderByDescending(s => s.Date)
            .Select(s => new CommissionistSaleDto
            {
                Id = s.Id,
                Date = s.Date,
                CustomerName = $"{s.Customer.Name} {s.Customer.LastName}",
                ProductDescription = s.ProductDescription,
                TotalAmount = s.TotalAmount,
                IsPaid = s.IsPaid,
                CommissionAmount = s.CommissionAmount,
                IsCommissionPaid = s.IsCommissionPaid,
                CommissionPaidAt = s.CommissionPaidAt,
                Payments = s.Payment
                    .OrderByDescending(p => p.Date)
                    .Select(p => new PaymentDto
                    {
                        Id = p.Id,
                        SaleId = p.SaleId,
                        Amount = p.Amount,
                        Date = p.Date,
                        PaymentMethod = p.PaymentMethod,
                        Reference = p.Reference,
                        PaymentTypeId = p.PaymentTypeId
                    }).ToList()
            })
            .ToListAsync(cancellationToken);
    }
}