using BusinessCloud.Application.Admin.Dtos;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Common.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Admin.Queries.GetCompanyById;

/// <summary>
/// Obtiene el detalle de una empresa y su suscripción. Devuelve null si no existe.
/// </summary>
public record GetCompanyByIdQuery(string TenantId) : IRequest<CompanyDetailDto?>;

public class GetCompanyByIdHandler(IIdentityDbContext context)
    : IRequestHandler<GetCompanyByIdQuery, CompanyDetailDto?>
{
    private readonly IIdentityDbContext _context = context;

    public async Task<CompanyDetailDto?> Handle(
        GetCompanyByIdQuery request,
        CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);

        if (tenant is null)
            return null;

        var modules = await _context.TenantModules
            .AsNoTracking()
            .Where(m => m.TenantId == tenant.Id && m.IsActive)
            .Select(m => m.Module)
            .ToListAsync(cancellationToken);

        var subscription = await _context.TenantSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenant.Id, cancellationToken);

        var userCount = await _context.Users
            .AsNoTracking()
            .CountAsync(u => u.TenantId == tenant.Id, cancellationToken);

        var balance = await _context.TenantMessageBalances
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.TenantId == tenant.Id, cancellationToken);

        var now = DateTime.UtcNow;

        var dto = new CompanyDetailDto
        {
            TenantId = tenant.Id,
            Name = tenant.Name,
            IsActive = tenant.IsActive,
            CreatedAt = tenant.CreatedAt,
            Modules = modules,
            UserCount = userCount,
            HasSubscription = subscription is not null,
            MessagesAvailable = balance?.Available ?? 0,
            MessagesTotalPurchased = balance?.TotalPurchased ?? 0,
            MessagesTotalUsed = balance?.TotalUsed ?? 0,
        };

        if (subscription is not null)
        {
            dto.SubscriptionId = subscription.Id;
            dto.PlanName = subscription.PlanName;
            dto.Period = subscription.Period.ToString();
            dto.Price = subscription.Price;
            dto.Currency = subscription.Currency;
            dto.StartDate = subscription.StartDate;
            dto.PaidUntil = subscription.PaidUntil;
            dto.GraceDays = subscription.GraceDays;
            dto.GraceEndsOn = subscription.GraceEndsOn;
            dto.IsManuallySuspended = subscription.IsManuallySuspended;
            dto.Status = subscription.EvaluateStatus(now).ToString();
            dto.DaysUntilExpiration = subscription.DaysUntilExpiration(now);
            dto.OwnerName = subscription.OwnerName;
            dto.OwnerPhone = subscription.OwnerPhone;
            dto.SellerId = subscription.SellerId;
            dto.CommissionInitialAmount = subscription.CommissionInitialAmount;
            dto.CommissionMonthlyPercent = subscription.CommissionMonthlyPercent;
            dto.Notes = subscription.Notes;
        }
        else
        {
            dto.Status = SubscriptionStatus.Suspended.ToString();
        }

        return dto;
    }
}
