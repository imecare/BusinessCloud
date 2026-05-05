using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using BusinessCloud.Application.Common.Interfaces;

namespace BusinessCloud.Application.Auth.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? UserId =>
            _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        public string? Username =>
            _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name);

        public string? Role =>
            _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role);

        public string? TenantId =>
            _httpContextAccessor.HttpContext?.User?.FindFirstValue("tenant_id");

        public int? SellerId
        {
            get
            {
                var value = _httpContextAccessor.HttpContext?.User?.FindFirstValue("seller_id");
                return int.TryParse(value, out var id) ? id : null;
            }
        }
    }
}
