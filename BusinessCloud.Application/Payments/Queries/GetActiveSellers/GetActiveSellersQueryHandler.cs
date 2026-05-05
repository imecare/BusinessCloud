using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Payments.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Queries.GetActiveSellers;

public class GetActiveSellersQueryHandler : IRequestHandler<GetActiveSellersQuery, List<SellerDto>>
{
    private readonly IPaymentsDbContext _db;

    public GetActiveSellersQueryHandler(IPaymentsDbContext db) => _db = db;

    public async Task<List<SellerDto>> Handle(GetActiveSellersQuery request, CancellationToken cancellationToken)
    {
        return await _db.Sellers
            .AsNoTracking()
            .Where(s => s.StatusId == 1)
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