using BusinessCloud.Domain.Common.Entities;
using MediatR;

namespace BusinessCloud.Application.Admin.Commands.UpsertPackage;

/// <summary>Crea (Id nulo) o actualiza un paquete del catálogo. Devuelve su Id.</summary>
public record UpsertPackageCommand : IRequest<int>
{
    public int? Id { get; init; }
    public string Name { get; init; } = null!;
    public string Module { get; init; } = SystemModules.Bazares;
    public decimal Price { get; init; }
    public string Currency { get; init; } = "MXN";
    public int IncludedMessages { get; init; }
    public bool IsActive { get; init; } = true;
    public string? Description { get; init; }
}
