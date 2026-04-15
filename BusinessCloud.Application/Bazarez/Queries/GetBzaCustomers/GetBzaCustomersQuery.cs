using MediatR;
using Microsoft.EntityFrameworkCore;
using BusinessCloud.Application.Common.Interfaces;

namespace BusinessCloud.Application.Bazares.Queries.GetBzaCustomers;

public record BzaCustomerDto(int Id, string Name, string Phone, string? FacebookName, int Status, string CollectorName);

public record GetBzaCustomersQuery : IRequest<List<BzaCustomerDto>>;

public class GetBzaCustomersHandler : IRequestHandler<GetBzaCustomersQuery, List<BzaCustomerDto>>
{
    private readonly IBazaresDbContext _context;
    public GetBzaCustomersHandler(IBazaresDbContext context) => _context = context;

    public async Task<List<BzaCustomerDto>> Handle(GetBzaCustomersQuery request, CancellationToken ct)
    {
        return await _context.Customers
            .Include(c => c.Collector) // Join con Recolectores
            .Select(c => new BzaCustomerDto(c.Id, c.Name, c.Phone, c.FacebookName, c.Status, c.Collector.Name))
            .ToListAsync(ct);
    }
}