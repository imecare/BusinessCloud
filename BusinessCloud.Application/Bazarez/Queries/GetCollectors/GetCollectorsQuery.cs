using MediatR;
using Microsoft.EntityFrameworkCore;
using BusinessCloud.Application.Common.Interfaces;

namespace BusinessCloud.Application.Bazares.Queries.GetCollectors;

public record CollectorDto(int Id, string Name, string? FacebookName, string? GroupId);

public record GetCollectorsQuery : IRequest<List<CollectorDto>>;

public class GetCollectorsHandler : IRequestHandler<GetCollectorsQuery, List<CollectorDto>>
{
    private readonly IBazaresDbContext _context;
    public GetCollectorsHandler(IBazaresDbContext context) => _context = context;

    public async Task<List<CollectorDto>> Handle(GetCollectorsQuery request, CancellationToken ct)
    {
        return await _context.Collectors
            .Select(c => new CollectorDto(c.Id, c.Name, c.FacebookName, c.GroupId))
            .ToListAsync(ct);
    }
}