using System.Security.Claims; // Reemplaza a IdentityModel
using Microsoft.AspNetCore.Http;
using BusinessCloud.Application.Common.Interfaces;

namespace BusinessCloud.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    // Asegúrate de que el nombre sea TenantId (con T mayúscula)
    public string? TenantId => _httpContextAccessor.HttpContext?.User?.FindFirst("tenant_id")?.Value;
    public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    public string? Username => _httpContextAccessor.HttpContext?.User?.Identity?.Name;
    public string? Role => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;
}