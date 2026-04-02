using BusinessCloud.Domain.Commissions.Entities;

namespace BusinessCloud.Application.Auth.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerateToken(InfluenceCenter user);
    }
}
