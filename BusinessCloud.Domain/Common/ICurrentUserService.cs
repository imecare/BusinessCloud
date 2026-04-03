

namespace BusinessCloud.Domain.Common
{
    public interface ICurrentUserService
    {
        string? UserId { get; }
        string? Username { get; }
        string? Role { get; }
        int TenantId { get; }
    }
}
