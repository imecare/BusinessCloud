using Microsoft.AspNetCore.Identity;


namespace BusinessCloud.Domain.Common.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string TenantId { get; set; } = null!; // El claim que viajará en el JWT
    }
}
