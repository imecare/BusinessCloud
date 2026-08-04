using MediatR;

namespace BusinessCloud.Application.Bazares.Queries.GetGlobalCollectorGroups;

public record GlobalCollectorDto(int Id, string Name);

public record GlobalCollectorGroupDto(
    int Id,
    string Description,
    string? DeliveryFrequency,
    int? DeliveryDay,
    int CollectorCount,
    IReadOnlyList<GlobalCollectorDto> Collectors);

public record GetGlobalCollectorGroupsQuery : IRequest<IReadOnlyList<GlobalCollectorGroupDto>>;
