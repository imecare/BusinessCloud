using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Payments.Entities
{
    public class Customer : IAuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string RFC { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        // Relación con el vendedor (ayudante) que atiende al cliente
        public int SellerId { get; set; }

        // Propiedades de auditoría (IAuditableEntity)
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }

        // Relación de navegación
        public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
    }
}
