
namespace BusinessCloud.Domain.Common.Entities
{
    public class Tenant
    {
        public string Id { get; set; } = null!; // El identificador único (ej: "empresa-abc")
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
