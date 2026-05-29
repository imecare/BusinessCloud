namespace BusinessCloud.Application.Common.Interfaces;

public interface ICurrentUserService
{
    string? TenantId { get; }
    string? UserId { get; }
    string? Username { get; }
    string? Role { get; }
    int? SellerId { get; }
    
    /// <summary>
    /// Returns TenantId or throws TenantResolutionException if not available.
    /// Use this method when TenantId is mandatory for the operation.
    /// </summary>
    string GetRequiredTenantId();
}