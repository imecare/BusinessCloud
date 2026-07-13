using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.MergeBzaCustomers;

/// <summary>
/// Resultado de la fusión de clientes duplicados.
/// </summary>
public record MergeBzaCustomersResultDto(
    int SurvivorId,
    int MergedCount,
    int MovedSales,
    int MovedPayments,
    int MovedClosureTotals);

/// <summary>
/// Fusiona uno o varios clientes duplicados (<see cref="MergeIds"/>) dentro del
/// cliente que se conserva (<see cref="SurvivorId"/>). Reasigna todo el historial
/// (ventas, pagos y totales de cierre) al cliente conservado, aplica los datos
/// elegidos por el usuario y elimina los clientes duplicados.
/// </summary>
public record MergeBzaCustomersCommand(
    int SurvivorId,
    IReadOnlyList<int> MergeIds,
    string Name,
    string Phone,
    string? FacebookName,
    int Status,
    int BzaCollectorId) : IRequest<MergeBzaCustomersResultDto>;
