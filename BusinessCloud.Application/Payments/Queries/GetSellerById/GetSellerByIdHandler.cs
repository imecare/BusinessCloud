using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Payments.Dtos;
using BusinessCloud.Application.Payments.Queries.GetCustomerById;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Queries.GetSellerById
{

    public class GetSellerByIdHandler : IRequestHandler<GetSellerByIdQuery, SellerDto?>
    {
        private readonly IPaymentsDbContext _db;

        public GetSellerByIdHandler(IPaymentsDbContext db) => _db = db;

        public async Task<SellerDto?> Handle(GetSellerByIdQuery request, CancellationToken cancellationToken)
        {
            var c = await _db.Sellers
                             .AsNoTracking()
                             .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (c == null) return null;

            return new SellerDto
            {
                Id = c.Id,
                Name = c.Name,
                LastName = c.LastName,
                Phone = c.Phone,
            };
        }
    }
}
