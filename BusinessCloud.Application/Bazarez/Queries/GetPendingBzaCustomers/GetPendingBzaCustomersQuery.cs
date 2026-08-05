using MediatR;
using Microsoft.EntityFrameworkCore;
using BusinessCloud.Application.Common.Interfaces;

namespace BusinessCloud.Application.Bazares.Queries.GetPendingBzaCustomers;

public record PendingBzaCustomerDto(
    int Id,
    string Name,
    string Phone,
    string? FacebookName,
    int Status,
    string CollectorName,
    bool IsBlocked,
    bool IsPendingInfo,
    bool HasNoWhatsApp);

public record PendingBzaCustomersPageDto(List<PendingBzaCustomerDto> Items, int TotalCount);

public record GetPendingBzaCustomersQuery(string? Query, int Skip = 0, int Take = 200) : IRequest<PendingBzaCustomersPageDto>;

public class GetPendingBzaCustomersHandler : IRequestHandler<GetPendingBzaCustomersQuery, PendingBzaCustomersPageDto>
{
    private readonly IBazaresDbContext _context;

    public GetPendingBzaCustomersHandler(IBazaresDbContext context) => _context = context;

    public async Task<PendingBzaCustomersPageDto> Handle(GetPendingBzaCustomersQuery request, CancellationToken ct)
    {
        var raw = (request.Query ?? string.Empty).Trim();
        var query = _context.Customers
            .AsNoTracking()
            .Include(c => c.Collector)
            .Where(c => c.IsPendingInfo);

        if (!string.IsNullOrWhiteSpace(raw))
        {
            query = query.Where(c => c.Name.Contains(raw));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Skip(Math.Max(0, request.Skip))
            .Take(request.Take <= 0 ? 200 : request.Take)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Phone,
                c.FacebookName,
                c.Status,
                CollectorName = c.Collector != null ? c.Collector.Name : string.Empty,
                c.IsPendingInfo,
                c.HasNoWhatsApp,
            })
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

        return new PendingBzaCustomersPageDto(
            items.Select(c => new PendingBzaCustomerDto(
                c.Id,
                c.Name,
                c.Phone,
                c.FacebookName,
                c.Status,
                c.CollectorName,
                blockedIds.Contains(c.Id)
                    || blockedNames.Contains((c.Name ?? string.Empty).Trim().ToLower())
                    || (!string.IsNullOrWhiteSpace(c.FacebookName) && blockedFbs.Contains(c.FacebookName!.Trim().ToLower())),
                c.IsPendingInfo,
                c.HasNoWhatsApp
            )).ToList(),
            totalCount
        );
    }
}
