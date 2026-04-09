namespace BusinessCloud.Application.Payments.Dtos
{
    public class CreateSaleRequest
    {
        public int CustomerId { get; set; }
        public int SellerId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal ProductCost { get; set; }
    }

    public class SaleResponse
    {
        public int Id { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal RemainingBalance { get; set; } // Saldo pendiente
        public bool IsPaid { get; set; }
    }
}
