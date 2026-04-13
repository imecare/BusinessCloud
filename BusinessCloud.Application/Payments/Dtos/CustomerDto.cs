
namespace BusinessCloud.Application.Payments.Dtos
{
    public class CustomerDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string RFC { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int SellerId { get; set; }
        public string SellerName { get; set; } = string.Empty; // <-- Añadido
    }
}
