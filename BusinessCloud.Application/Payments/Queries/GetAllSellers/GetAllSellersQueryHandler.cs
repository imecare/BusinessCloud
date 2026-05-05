using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Payments.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Queries.GetAllSellers;

public class GetAllSellersQueryHandler : IRequestHandler<GetAllSellersQuery, List<SellerDto>>
{
    private readonly IPaymentsDbContext _db;

    public GetAllSellersQueryHandler(IPaymentsDbContext db) => _db = db;

    public async Task<List<SellerDto>> Handle(GetAllSellersQuery request, CancellationToken cancellationToken)
    {
        return await _db.Sellers
            .AsNoTracking()
            .Select(s => new SellerDto
            {
                Id = s.Id,
                Name = s.Name,
                LastName = s.LastName,
                Phone = s.Phone,
                StatusId = s.StatusId
            })
            .ToListAsync(cancellationToken);
    }
}