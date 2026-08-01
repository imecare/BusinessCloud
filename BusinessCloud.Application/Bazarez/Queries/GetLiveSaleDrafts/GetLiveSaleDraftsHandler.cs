using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetLiveSaleDrafts;

public class GetLiveSaleDraftsHandler(IBazaresDbContext context)
    : IRequestHandler<GetLiveSaleDraftsQuery, List<LiveSaleDraftDto>>
{
    public async Task<List<LiveSaleDraftDto>> Handle(GetLiveSaleDraftsQuery request, CancellationToken ct)
        => await context.LiveSaleDrafts.AsNoTracking()
            .Where(x => x.BzaEventId == request.BzaEventId)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new LiveSaleDraftDto(x.Id, x.BzaEventId, x.Description, x.Price, x.CreatedAt))
            .ToListAsync(ct);
}
