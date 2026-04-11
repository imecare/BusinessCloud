using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Payments.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Queries.GetAllCustomers;

public class GetAllCustomersQueryHandler : IRequestHandler<GetAllCustomersQuery, IEnumerable<CustomerDto>>
{
    private readonly IPaymentsDbContext _context;

    public GetAllCustomersQueryHandler(IPaymentsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CustomerDto>> Handle(GetAllCustomersQuery request, CancellationToken cancellationToken)
    {
        var customers = await _context.Customers
            .AsNoTracking()
            .Select(c => new CustomerDto
            {
                Id = c.Id,
                Name = c.Name,
                LastName = c.LastName, // Ajusta si en tu entidad se llama distinto
                RFC = c.RFC,
                Phone = c.Phone,
                SellerId = c.SellerId
            })
            .ToListAsync(cancellationToken);

        return customers;
    }
}