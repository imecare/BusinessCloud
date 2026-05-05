using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Payments.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Queries.GetMyCustomers;

public class GetMyCustomersQueryHandler : IRequestHandler<GetMyCustomersQuery, List<CustomerDto>>
{
    private readonly IPaymentsDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMyCustomersQueryHandler(IPaymentsDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<CustomerDto>> Handle(GetMyCustomersQuery request, CancellationToken cancellationToken)
    {
        var sellerId = _currentUser.SellerId
            ?? throw new UnauthorizedAccessException("No se pudo determinar el SellerId del token.");

        return await _db.Customers
            .AsNoTracking()
            .Where(c => c.SellerId == sellerId)
            .Include(c => c.Seller)
            .Select(c => new CustomerDto
            {
                Id = c.Id,
                Name = c.Name,
                LastName = c.LastName,
                RFC = c.RFC,
                Phone = c.Phone,
                SellerId = c.SellerId,
                SellerName = $"{c.Seller.Name} {c.Seller.LastName}"
            })
            .ToListAsync(cancellationToken);
    }
}