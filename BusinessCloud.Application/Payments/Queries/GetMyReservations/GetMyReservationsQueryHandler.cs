using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Payments.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Queries.GetMyReservations;

public class GetMyReservationsQueryHandler : IRequestHandler<GetMyReservationsQuery, List<ReservationDto>>
{
    private readonly IPaymentsDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMyReservationsQueryHandler(IPaymentsDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<ReservationDto>> Handle(GetMyReservationsQuery request, CancellationToken cancellationToken)
    {
        var sellerId = _currentUser.SellerId
            ?? throw new UnauthorizedAccessException("No se pudo determinar el SellerId del token.");

        return await _db.Reservations
            .AsNoTracking()
            .Where(r => r.SellerId == sellerId)
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
