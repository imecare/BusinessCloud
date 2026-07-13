using MediatR;
using Microsoft.EntityFrameworkCore;
using BusinessCloud.Application.Common.Interfaces;

namespace BusinessCloud.Application.Bazares.Queries.GetBzaCustomers;

public record BzaCustomerDto(int Id, string Name, string Phone, string? FacebookName, int Status, string CollectorName, bool IsBlocked);

public record GetBzaCustomersQuery : IRequest<List<BzaCustomerDto>>;

public class GetBzaCustomersHandler : IRequestHandler<GetBzaCustomersQuery, List<BzaCustomerDto>>
{
    private readonly IBazaresDbContext _context;
    public GetBzaCustomersHandler(IBazaresDbContext context) => _context = context;

    public async Task<List<BzaCustomerDto>> Handle(GetBzaCustomersQuery request, CancellationToken ct)
    {
        var customers = await _context.Customers
            .Include(c => c.Collector)
            .Select(c => new { c.Id, c.Name, c.Phone, c.FacebookName, c.Status, CollectorName = c.Collector.Name })
            .ToListAsync(ct);

        var blocks = await _context.BlockedCustomers
            .AsNoTracking()
            .Where(b => b.IsActive)
            .Select(b => new { b.BzaCustomerId, b.Name, b.FacebookName })
            .ToListAsync(ct);

        var blockedIds = new HashSet<int>(blocks.Where(b => b.BzaCustomerId.HasValue).Select(b => b.BzaCustomerId!.Value));
        var blockedNames = new HashSet<string>(blocks.Select(b => (b.Name ?? string.Empty).Trim().ToLower()));
        var blockedFbs = new HashSet<string>(blocks
            .Where(b => !string.IsNullOrWhiteSpace(b.FacebookName))
            .Select(b => b.FacebookName!.Trim().ToLower()));

        return customers.Select(c => new BzaCustomerDto(
            c.Id, c.Name, c.Phone, c.FacebookName, c.Status, c.CollectorName,
            blockedIds.Contains(c.Id)
                || blockedNames.Contains((c.Name ?? string.Empty).Trim().ToLower())
                || (!string.IsNullOrWhiteSpace(c.FacebookName) && blockedFbs.Contains(c.FacebookName!.Trim().ToLower()))
        )).ToList();
    }
}