using BusinessCloud.Domain.Common.Entities;
using MediatR;

namespace BusinessCloud.Application.Admin.Commands.UpsertSubscription;

/// <summary>
/// Crea o actualiza la suscripción de una empresa (plan, precio, fecha pagada y prórroga).
/// Devuelve el Id de la suscripción.
/// </summary>
public record UpsertSubscriptionCommand : IRequest<int>
{
    public string TenantId { get; init; } = null!;
    public string PlanName { get; init; } = "Mensual";
    public BillingPeriod Period { get; init; } = BillingPeriod.Monthly;
    public decimal Price { get; init; }
    public string Currency { get; init; } = "MXN";
    public DateTime PaidUntil { get; init; }
    public int GraceDays { get; init; } = 5;
    public string? OwnerName { get; init; }
    public string? OwnerPhone { get; init; }
    public int? SellerId { get; init; }
    public decimal CommissionInitialAmount { get; init; }
    public decimal CommissionMonthlyPercent { get; init; }
    public string? Notes { get; init; }
}
