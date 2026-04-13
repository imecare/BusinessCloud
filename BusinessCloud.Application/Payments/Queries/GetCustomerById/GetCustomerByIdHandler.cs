using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Payments.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Queries.GetCustomerById;

public class GetCustomerByIdQueryHandler : IRequestHandler<GetCustomerByIdQuery, CustomerDto?>
{
    private readonly IPaymentsDbContext _context;

    public GetCustomerByIdQueryHandler(IPaymentsDbContext context)
    {
        _context = context;
    }

    public async Task<CustomerDto?> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {       
        var customerDto = await _context.Customers
                             .AsNoTracking()
                             .Include(s => s.Seller)
                             .Where(c => c.Id == request.Id)
                             .Select(s => new CustomerDto
                             {
                                 Id = s.Id,
                                 Name = s.Name,
                                 LastName = s.LastName,
                                 RFC = s.RFC,
                                 Phone = s.Phone,
                                 SellerId = s.SellerId,
                                 SellerName = s.Seller.Name + ' ' + s.Seller.LastName
                             })
                             .FirstOrDefaultAsync(cancellationToken);

        return customerDto;
    }
}