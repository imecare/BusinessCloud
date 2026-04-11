using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Payments.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Queries.GetCustomerById
{

    public class GetAllCustomersQueryHandler : IRequestHandler<GetCustomerByIdQuery, CustomerDto?>
    {
        private readonly IPaymentsDbContext _db;

        public GetAllCustomersQueryHandler(IPaymentsDbContext db) => _db = db;

        public async Task<CustomerDto?> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
        {
            var c = await _db.Customers
                             .AsNoTracking()
                             .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (c == null) return null;

            return new CustomerDto
            {
                Id = c.Id,
                Name = c.Name,
                RFC = c.RFC,
                Phone = c.Phone,
                SellerId = c.SellerId
            };
        }
    }
}
