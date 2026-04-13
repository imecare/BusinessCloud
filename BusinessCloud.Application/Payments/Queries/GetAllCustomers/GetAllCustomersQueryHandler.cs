using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Payments.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Queries.GetAllCustomers
{
    namespace BusinessCloud.Application.Payments.Queries.GetAllCustomers // Corregido el namespace
    {
        // Cambiado IRequestHandler para recibir GetAllCustomersQuery y devolver List<CustomerDto>
        public class GetAllCustomersQueryHandler : IRequestHandler<GetAllCustomersQuery, List<CustomerDto>>
        {
            private readonly IPaymentsDbContext _db;

            public GetAllCustomersQueryHandler(IPaymentsDbContext db) => _db = db;

            // Cambiado el tipo request a GetAllCustomersQuery
            public async Task<List<CustomerDto>> Handle(GetAllCustomersQuery request, CancellationToken cancellationToken)
            {
                var customerDtos = await _db.Customers
                                 .AsNoTracking()
                                 .Include(s => s.Seller)
                                 .Select(s => new CustomerDto
                                 {
                                     Id = s.Id,
                                     Name = s.Name,
                                     LastName = s.LastName,
                                     RFC = s.RFC,
                                     Phone = s.Phone,
                                     SellerId = s.SellerId,
                                     SellerName = s.Seller.Name + " " + s.Seller.LastName // Usar comillas dobles para strings
                                 }
                                 ).ToListAsync(cancellationToken);

                return customerDtos;
            }
        }
    }
}
