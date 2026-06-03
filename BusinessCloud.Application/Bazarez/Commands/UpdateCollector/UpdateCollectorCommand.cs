using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.UpdateCollector;

public record UpdateCollectorCommand : IRequest
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? FacebookName { get; init; }
    public int? BzaCollectorGroupId { get; init; }
}