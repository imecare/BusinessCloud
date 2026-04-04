using BusinessCloud.Application.Common.Interfaces; // Esto fallará hasta que hagas el Paso 1

namespace BusinessCloud.Infrastructure.Data
{
    public class DummyCurrentUserService : ICurrentUserService
    {
        public string? UserId => "Admin_Test_ID";
        public string? Username => "admin@test.com";
        public string? Role => "Admin";
        public string? TenantId => "1";
    }
}
