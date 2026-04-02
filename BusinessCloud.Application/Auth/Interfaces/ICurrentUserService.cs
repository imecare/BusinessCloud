

namespace BusinessCloud.Application.Auth.Interfaces
{
    public interface ICurrentUserService
    {
        string? UserId { get; }
        string? Username { get; }
        string? Role { get; }
    }
}
