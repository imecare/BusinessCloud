using BusinessCloud.Application.Common.Interfaces;


namespace BusinessCloud.Application.Common.Interfaces;


public interface ICurrentUserService
{
    string? TenantId { get; }
    string? UserId { get; }
    string? Username { get; } // Agregado
    string? Role { get; }     // Agregado
}