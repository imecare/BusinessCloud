using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Payments.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Queries.GetSellerById;

public class GetSellerByIdQueryHandler : IRequestHandler<GetSellerByIdQuery, SellerDto?>
{
    private readonly IPaymentsDbContext _db;

    public GetSellerByIdQueryHandler(IPaymentsDbContext db) => _db = db;

    public async Task<SellerDto?> Handle(GetSellerByIdQuery request, CancellationToken cancellationToken)
    {
        return await _db.Sellers
            .AsNoTracking()
            .Where(s => s.Id == request.Id)
            .Select(s => new SellerDto
            {
                Id = s.Id,
                Name = s.Name,
                LastName = s.LastName,
                Phone = s.Phone,
                StatusId = s.StatusId
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}