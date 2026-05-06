using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Payments.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Queries.GetAllSellers;

public class GetAllSellersHandler : IRequestHandler<GetAllSellersQuery, List<SellerDto>>
{
    private readonly IPaymentsDbContext _context;

    public GetAllSellersHandler(IPaymentsDbContext context)
    {
        _context = context;
    }

    public async Task<List<SellerDto>> Handle(GetAllSellersQuery request, CancellationToken cancellationToken)
    {
        return await _context.Sellers
            .AsNoTracking()
            .Select(seller => new SellerDto
            {
                Id = seller.Id,
                Name = seller.Name,
                LastName = seller.LastName,
                Phone = seller.Phone,
                StatusId = seller.StatusId
            })
            .ToListAsync(cancellationToken);
    }
}