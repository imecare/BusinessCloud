using BusinessCloud.Application.Admin.Dtos;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Common.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Admin.Queries.GetCompanies;

/// <summary>
/// Lista todas las empresas con el estado de su suscripción.
/// Opcionalmente filtra por texto (nombre/id) y por estado de suscripción.
/// </summary>
public record GetCompaniesQuery(string? Search = null, string? Status = null)
    : IRequest<IReadOnlyList<CompanyListItemDto>>;

public class GetCompaniesHandler(IIdentityDbContext context)
    : IRequestHandler<GetCompaniesQuery, IReadOnlyList<CompanyListItemDto>>
{
    private readonly IIdentityDbContext _context = context;

    public async Task<IReadOnlyList<CompanyListItemDto>> Handle(
        GetCompaniesQuery request,
        CancellationToken cancellationToken)
    {
        var tenantsQuery = _context.Tenants.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            tenantsQuery = tenantsQuery.Where(t =>
                t.Name.ToLower().Contains(term) || t.Id.ToLower().Contains(term));
        }

        var tenants = await tenantsQuery
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.IsActive,
                t.CreatedAt,
                Modules = _context.TenantModules
                    .Where(m => m.TenantId == t.Id && m.IsActive)
                    .Select(m => m.Module)
                    .ToList(),
                Subscription = _context.TenantSubscriptions
                    .FirstOrDefault(s => s.TenantId == t.Id),
            })
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;

        var result = tenants
            .Select(t => MapToDto(t.Id, t.Name, t.IsActive, t.CreatedAt, t.Modules, t.Subscription, now))
            .ToList();

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = request.Status.Trim();
            result = result
                .Where(r => string.Equals(r.Status, status, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return result
            .OrderBy(r => r.Name)
            .ToList();
    }

    internal static CompanyListItemDto MapToDto(
        string tenantId,
        string name,
        bool isActive,
        DateTime createdAt,
        IReadOnlyList<string> modules,
        TenantSubscription? subscription,
        DateTime now)
    {
        var dto = new CompanyListItemDto
        {
            TenantId = tenantId,
            Name = name,
            IsActive = isActive,
            CreatedAt = createdAt,
            Modules = modules,
            HasSubscription = subscription is not null,
        };

        if (subscription is not null)
        {
            dto.PlanName = subscription.PlanName;
            dto.Period = subscription.Period.ToString();
            dto.Price = subscription.Price;
            dto.Currency = subscription.Currency;
            dto.PaidUntil = subscription.PaidUntil;
            dto.GraceDays = subscription.GraceDays;
            dto.GraceEndsOn = subscription.GraceEndsOn;
            dto.Status = subscription.EvaluateStatus(now).ToString();
            dto.DaysUntilExpiration = subscription.DaysUntilExpiration(now);
            dto.OwnerName = subscription.OwnerName;
            dto.OwnerPhone = subscription.OwnerPhone;
            dto.SellerId = subscription.SellerId;
        }
        else
        {
            dto.Status = SubscriptionStatus.Suspended.ToString();
        }

        return dto;
    }
}
