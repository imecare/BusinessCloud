
namespace BusinessCloud.Domain.Common.Entities
{
    public class Tenant
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<TenantModule> Modules { get; set; } = new List<TenantModule>();
    }

    /// <summary>
    /// Nombres válidos de módulos del sistema.
    /// </summary>
    public static class SystemModules
    {
        public const string Payments = "Payments";
        public const string Bazares = "Bazares";
        public const string Commissions = "Commissions";

        public static readonly string[] All = [Payments, Bazares, Commissions];
    }
}
