using BusinessCloud.Application.Admin.Dtos;
using BusinessCloud.Application.Admin.Queries.GetCompanies;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Common.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Admin.Queries.GetExpirationAlerts;

/// <summary>
/// Devuelve los contadores de vencimiento y la lista de empresas que requieren atención
/// (por vencer, en prórroga o suspendidas).
/// </summary>
public record GetExpirationAlertsQuery(int ExpiringSoonDays = 10)
    : IRequest<ExpirationAlertsDto>;

public class GetExpirationAlertsHandler(IIdentityDbContext context)
    : IRequestHandler<GetExpirationAlertsQuery, ExpirationAlertsDto>
{
    private readonly IIdentityDbContext _context = context;

    public async Task<ExpirationAlertsDto> Handle(
        GetExpirationAlertsQuery request,
        CancellationToken cancellationToken)
    {
        var tenants = await _context.Tenants
            .AsNoTracking()
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
        var dto = new ExpirationAlertsDto();
        var needAttention = new List<CompanyListItemDto>();

        foreach (var t in tenants)
        {
            if (t.Subscription is null)
                continue;

            dto.TotalWithSubscription++;
            var status = t.Subscription.EvaluateStatus(now, request.ExpiringSoonDays);

            switch (status)
            {
                case SubscriptionStatus.Active:
                    dto.ActiveCount++;
                    break;
                case SubscriptionStatus.ExpiringSoon:
                    dto.ExpiringSoonCount++;
                    break;
                case SubscriptionStatus.Grace:
                    dto.GraceCount++;
                    break;
                case SubscriptionStatus.Suspended:
                    dto.SuspendedCount++;
                    break;
            }

            if (status != SubscriptionStatus.Active)
            {
                needAttention.Add(GetCompaniesHandler.MapToDto(
                    t.Id, t.Name, t.IsActive, t.CreatedAt, t.Modules, t.Subscription, now));
            }
        }

        dto.Companies = needAttention
            .OrderBy(c => c.DaysUntilExpiration)
            .ToList();

        return dto;
    }
}
