using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Payments.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Queries.GetAllReservations;

public class GetAllReservationsQueryHandler : IRequestHandler<GetAllReservationsQuery, List<ReservationDto>>
{
    private readonly IPaymentsDbContext _db;

    public GetAllReservationsQueryHandler(IPaymentsDbContext db) => _db = db;

    public async Task<List<ReservationDto>> Handle(GetAllReservationsQuery request, CancellationToken cancellationToken)
    {
        return await _db.Reservations
            .AsNoTracking()
            .Include(r => r.Customer)
            .Include(r => r.Seller)
            .OrderByDescending(r => r.Date)
            .Select(r => new ReservationDto
            {
                Id = r.Id,
                Date = r.Date,
                CustomerId = r.CustomerId,
                CustomerName = $"{r.Customer.Name} {r.Customer.LastName}",
                SellerId = r.SellerId,
                SellerName = r.Seller != null ? $"{r.Seller.Name} {r.Seller.LastName}" : null,
                ProductDescription = r.ProductDescription,
                TotalAmount = r.TotalAmount,
                CostPrice = r.CostPrice,
                CommissionAmount = r.CommissionAmount
            })
            .ToListAsync(cancellationToken);
    }
}
