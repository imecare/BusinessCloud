using MediatR;

namespace BusinessCloud.Application.Admin.Commands.PurchasePackage;

/// <summary>
/// Registra la compra de un paquete (o de mensajes adicionales) para una empresa y
/// acumula los mensajes en su saldo. Se controla desde el módulo admin.
/// Si <see cref="PackageId"/> viene, se toman los mensajes/precio del catálogo;
/// en caso contrario se usan <see cref="CustomMessages"/> y <see cref="CustomPrice"/>.
/// </summary>
public record PurchasePackageCommand : IRequest<PurchasePackageResult>
{
    public string TenantId { get; init; } = null!;
    public int? PackageId { get; init; }
    public int? CustomMessages { get; init; }
    public decimal? CustomPrice { get; init; }
    public string? Note { get; init; }
}

public record PurchasePackageResult(
    string TenantId,
    int MessagesAdded,
    int Available,
    int TotalPurchased,
    int TotalUsed);
