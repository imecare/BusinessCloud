using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Common.Exceptions;

namespace BusinessCloud.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? TenantId => _httpContextAccessor.HttpContext?.User?.FindFirstValue("tenant_id");
    public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    public string? Username => _httpContextAccessor.HttpContext?.User?.Identity?.Name;
    public string? Role => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;

    public int? SellerId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User?.FindFirstValue("seller_id");
            return int.TryParse(value, out var id) ? id : null;
        }
    }

    public string GetRequiredTenantId()
    {
        var tenantId = TenantId;
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new TenantResolutionException();
        }
        return tenantId;
    }
}