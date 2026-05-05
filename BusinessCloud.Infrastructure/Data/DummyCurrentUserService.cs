using BusinessCloud.Application.Common.Interfaces;

namespace BusinessCloud.Infrastructure.Data;

public class DummyCurrentUserService : ICurrentUserService
{
    public string? UserId => "Admin_Test_ID";
    public string? Username => "admin@test.com";
    public string? Role => "Admin";
    public string? TenantId => "1";
    public int? SellerId => null;
}
