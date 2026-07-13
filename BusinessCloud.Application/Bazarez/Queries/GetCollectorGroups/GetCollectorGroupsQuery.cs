using MediatR;
using Microsoft.EntityFrameworkCore;
using BusinessCloud.Application.Common.Interfaces;

namespace BusinessCloud.Application.Bazares.Queries.GetCollectorGroups;

public record CollectorGroupDto(int Id, string Description, int? DeliveryDay, bool IsActive, int CollectorCount, int ActiveCollectorCount);

public record GetCollectorGroupsQuery(bool IncludeInactive = false) : IRequest<List<CollectorGroupDto>>;

public class GetCollectorGroupsHandler(IBazaresDbContext context)
    : IRequestHandler<GetCollectorGroupsQuery, List<CollectorGroupDto>>
{
    public async Task<List<CollectorGroupDto>> Handle(GetCollectorGroupsQuery request, CancellationToken ct)
    {
        var query = context.CollectorGroups.AsQueryable();

        if (!request.IncludeInactive)
        {
            query = query.Where(g => g.IsActive);
        }

        return await query
            .Select(g => new CollectorGroupDto(
                g.Id,
                g.Description,
                g.DeliveryDay,
                g.IsActive,
                g.Collectors.Count,
                g.Collectors.Count(c => c.IsActive)))
            .ToListAsync(ct);
    }
}
