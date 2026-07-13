using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.CreateCollector;

public record CreateCollectorCommand : IRequest<int>
{
    public string Name { get; init; } = string.Empty;
    public string? FacebookName { get; init; }
    public int? BzaCollectorGroupId { get; init; }

    /// <summary>Permite crear un recolector con nombre repetido si ya existe en OTRO grupo (bajo confirmación del usuario).</summary>
    public bool AllowDuplicateNameInOtherGroup { get; init; }
}