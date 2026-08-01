using MediatR;

namespace BusinessCloud.Application.Bazares.Queries.GetLiveSaleDrafts;

public record GetLiveSaleDraftsQuery(int BzaEventId) : IRequest<List<LiveSaleDraftDto>>;

public record LiveSaleDraftDto(int Id, int BzaEventId, string Description, decimal Price, DateTime CreatedAt);
