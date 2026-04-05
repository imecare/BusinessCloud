using BusinessCloud.Application.Auth.Dtos;

namespace BusinessCloud.Application.Auth.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
    }
}
