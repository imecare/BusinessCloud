using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Payments.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Queries.GetAllSales;

public class GetAllSalesQueryHandler : IRequestHandler<GetAllSalesQuery, List<AdminSaleDto>>
{
    private readonly IPaymentsDbContext _db;

    public GetAllSalesQueryHandler(IPaymentsDbContext db) => _db = db;

    public async Task<List<AdminSaleDto>> Handle(GetAllSalesQuery request, CancellationToken cancellationToken)
    {
        return await _db.Sales
            .AsNoTracking()
            .Include(s => s.Customer)
            .Include(s => s.Seller)
            .Include(s => s.Payment)
            .OrderByDescending(s => s.Date)
            .Select(s => new AdminSaleDto
            {
                Id = s.Id,
                Date = s.Date,
                CustomerId = s.CustomerId,
                CustomerName = $"{s.Customer.Name} {s.Customer.LastName}",
                SellerId = s.SellerId,
                SellerName = s.Seller != null ? $"{s.Seller.Name} {s.Seller.LastName}" : null,
                ProductDescription = s.ProductDescription,
                TotalAmount = s.TotalAmount,
                CostPrice = s.CostPrice,
                IsPaid = s.IsPaid,
                CommissionAmount = s.CommissionAmount,
                IsCommissionPaid = s.IsCommissionPaid,
                CommissionPaidAt = s.CommissionPaidAt,
                PaidAmount = s.Payment.Where(p => p.PaymentTypeId == 2).Sum(p => p.Amount),
                RemainingBalance = s.TotalAmount - s.Payment.Where(p => p.PaymentTypeId == 2).Sum(p => p.Amount) > 0
                    ? s.TotalAmount - s.Payment.Where(p => p.PaymentTypeId == 2).Sum(p => p.Amount)
                    : 0,
                PaymentProgress = s.TotalAmount > 0
                    ? Math.Min(100, s.Payment.Where(p => p.PaymentTypeId == 2).Sum(p => p.Amount) / s.TotalAmount * 100)
                    : 0,
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
