using MediatR;

namespace BusinessCloud.Application.Admin.Commands.UpsertSystemSeller;

/// <summary>
/// Crea (Id nulo) o actualiza un comisionista del SaaS. Devuelve su Id.
/// </summary>
public record UpsertSystemSellerCommand : IRequest<int>
{
    public int? Id { get; init; }
    public string Name { get; init; } = null!;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public bool IsActive { get; init; } = true;
    public decimal DefaultInitialAmount { get; init; }
    public decimal DefaultMonthlyPercent { get; init; }
}
