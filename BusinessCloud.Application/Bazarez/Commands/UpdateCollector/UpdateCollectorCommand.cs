using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.UpdateCollector;

public record UpdateCollectorCommand : IRequest
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? FacebookName { get; init; }
    public int? BzaCollectorGroupId { get; init; }

    /// <summary>Permite renombrar a un nombre repetido si ya existe en OTRO grupo (bajo confirmación del usuario).</summary>
    public bool AllowDuplicateNameInOtherGroup { get; init; }
}