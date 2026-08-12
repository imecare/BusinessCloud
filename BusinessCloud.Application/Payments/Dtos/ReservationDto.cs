namespace BusinessCloud.Application.Payments.Dtos
{
    /// <summary>Apartado (reserva de venta) listo para mostrarse en el front.</summary>
    public class ReservationDto
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int? SellerId { get; set; }
        public string? SellerName { get; set; }
        public string ProductDescription { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal CostPrice { get; set; }
        public decimal CommissionAmount { get; set; }
    }
}
