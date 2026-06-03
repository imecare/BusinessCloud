using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.CreateCollector;

public record CreateCollectorCommand : IRequest<int>
{
    public string Name { get; init; } = string.Empty;
    public string? FacebookName { get; init; }
    public int? BzaCollectorGroupId { get; init; }
}