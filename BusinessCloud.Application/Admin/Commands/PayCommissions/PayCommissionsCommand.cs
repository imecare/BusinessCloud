using MediatR;

namespace BusinessCloud.Application.Admin.Commands.PayCommissions;

/// <summary>
/// Marca como pagadas las comisiones de un comisionista. Si no se indican Ids,
/// se pagan todas las comisiones pendientes del comisionista.
/// </summary>
public record PayCommissionsCommand : IRequest<PayCommissionsResult>
{
    public int SystemSellerId { get; init; }
    public IReadOnlyList<int>? CommissionIds { get; init; }
    public string? Note { get; init; }
}

public record PayCommissionsResult(int PaidCount, decimal TotalPaid);
