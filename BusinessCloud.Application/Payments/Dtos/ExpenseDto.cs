namespace BusinessCloud.Application.Payments.Dtos
{
    public class ExpenseDto
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        /// <summary>"Cash" (contado) o "Installments" (a meses).</summary>
        public string PaymentType { get; set; } = "Cash";
        /// <summary>Número de meses cuando es a meses; null si es de contado.</summary>
        public int? Months { get; set; }
        /// <summary>Mensualidad proyectada (Cost / Months) cuando es a meses; null si es de contado.</summary>
        public decimal? MonthlyAmount { get; set; }
    }
}
