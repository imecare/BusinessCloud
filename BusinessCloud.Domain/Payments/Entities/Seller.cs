using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Payments.Entities
{
    public class Seller : BaseAuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int StatusId { get; set; } = 1; // 1 = Activo, 0 = Inactivo

        public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();

        public DateTime Date { get; set; }
    }
}
