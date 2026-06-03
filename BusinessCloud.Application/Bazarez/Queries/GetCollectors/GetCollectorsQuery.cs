using MediatR;
using Microsoft.EntityFrameworkCore;
using BusinessCloud.Application.Common.Interfaces;

namespace BusinessCloud.Application.Bazares.Queries.GetCollectors;

public record CollectorDto(int Id, string Name, string? FacebookName, bool IsActive, int? BzaCollectorGroupId, string? GroupDescription);

public record GetCollectorsQuery(bool IncludeInactive = false) : IRequest<List<CollectorDto>>;

public class GetCollectorsHandler : IRequestHandler<GetCollectorsQuery, List<CollectorDto>>
{
    private readonly IBazaresDbContext _context;
    public GetCollectorsHandler(IBazaresDbContext context) => _context = context;

    public async Task<List<CollectorDto>> Handle(GetCollectorsQuery request, CancellationToken ct)
    {
        var query = _context.Collectors
            .Include(c => c.CollectorGroup)
            .AsQueryable();

        if (!request.IncludeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query
            .Select(c => new CollectorDto(c.Id, c.Name, c.FacebookName, c.IsActive, c.BzaCollectorGroupId, c.CollectorGroup != null ? c.CollectorGroup.Description : null))
            .ToListAsync(ct);
    }
}