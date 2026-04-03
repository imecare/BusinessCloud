using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Payments.Entities
{
    public class Customer : BaseAuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string RFC { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        // Relación con el vendedor (ayudante) que atiende al cliente
        public int SellerId { get; set; }

      
        // Relación de navegación
        public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
    }
}
