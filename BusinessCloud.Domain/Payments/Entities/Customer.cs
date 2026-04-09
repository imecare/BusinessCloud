using BusinessCloud.Domain.Common;
using System.Text.Json.Serialization;

namespace BusinessCloud.Domain.Payments.Entities
{
    public class Customer : BaseAuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string RFC { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
       
        // Relación con el vendedor (ayudante) que atiende al cliente
        public int SellerId { get; set; }
        public virtual Seller Seller { get; set; } = null!;

        // Relación de navegación
        [JsonIgnore]
        public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
        // También oculta las de auditoría si vienen de la clase base
 

    }
}
