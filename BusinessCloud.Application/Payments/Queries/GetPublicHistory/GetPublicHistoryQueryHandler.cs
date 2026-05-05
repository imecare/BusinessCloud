using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Payments.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Queries.GetPublicHistory;

public class GetPublicHistoryQueryHandler : IRequestHandler<GetPublicHistoryQuery, PublicHistoryResult>
{
    private readonly IPaymentsDbContext _db;

    public GetPublicHistoryQueryHandler(IPaymentsDbContext db) => _db = db;

    public async Task<PublicHistoryResult> Handle(GetPublicHistoryQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.CompanyCode && t.IsActive, cancellationToken);

        if (tenant is null)
            return new PublicHistoryResult { CustomerFound = false, Data = null };

        var customer = await _db.Customers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.TenantId == tenant.Id &&
                c.Phone == request.Phone &&
                c.RFC == request.Rfc,
                cancellationToken);

        if (customer is null)
            return new PublicHistoryResult { CustomerFound = false, Data = null };

        var sales = await _db.Sales
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(s => s.TenantId == tenant.Id && s.CustomerId == customer.Id)
            .Include(s => s.Payment)
            .OrderByDescending(s => s.Date)
            .Select(s => new SaleHistoryDto
            {
                Id = s.Id,
                Date = s.Date,
                ProductDescription = s.ProductDescription,
                TotalAmount = s.TotalAmount,
                IsPaid = s.IsPaid,
                Payment = s.Payment
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

        return new PublicHistoryResult
        {
            CustomerFound = true,
            Data = new PublicHistoryLookupResponse
            {
                CustomerId = customer.Id,
                CustomerName = $"{customer.Name} {customer.LastName}",
                CompanyName = tenant.Name,
                HasMovements = sales.Count > 0,
                Sales = sales
            }
        };
    }
}