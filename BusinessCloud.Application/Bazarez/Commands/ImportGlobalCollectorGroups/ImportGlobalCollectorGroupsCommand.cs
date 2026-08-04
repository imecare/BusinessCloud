using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.ImportGlobalCollectorGroups;

public record ImportGlobalCollectorGroupsCommand(
    bool ImportAll,
    IReadOnlyCollection<int>? GroupIds) : IRequest<ImportGlobalCollectorGroupsResult>;

public record ImportedCollectorGroupResult(
    int GlobalGroupId,
    string Description,
    bool GroupCreated,
    int CollectorsCreated,
    int CollectorsSkipped);

public record ImportGlobalCollectorGroupsResult(
    int RequestedGroups,
    int GroupsCreated,
    int GroupsReused,
    int CollectorsCreated,
    int CollectorsSkipped,
    IReadOnlyList<ImportedCollectorGroupResult> Groups);
