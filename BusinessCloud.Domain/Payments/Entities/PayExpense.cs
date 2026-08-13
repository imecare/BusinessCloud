using BusinessCloud.Domain.Common;

namespace BusinessCloud.Domain.Payments.Entities
{
    /// <summary>
    /// Compra/Gasto del negocio. Puede pagarse de contado (Cash) o a meses (Installments).
    /// Cuando es a meses, <see cref="Months"/> indica en cuántas mensualidades se divide el costo.
    /// </summary>
    public class PayExpense : BaseAuditableEntity
    {
        public int Id { get; set; }

        /// <summary>Fecha del gasto/compra.</summary>
        public DateTime Date { get; set; }

        /// <summary>Descripción del gasto/compra.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Costo total gastado.</summary>
        public decimal Cost { get; set; }

        /// <summary>Forma de pago: "Cash" (contado) o "Installments" (a meses).</summary>
        public string PaymentType { get; set; } = ExpensePaymentTypes.Cash;

        /// <summary>Número de meses cuando el pago es a meses. Null si es de contado.</summary>
        public int? Months { get; set; }

        /// <summary>Indica si la compra/mercancía ya fue recibida.</summary>
        public bool IsReceived { get; set; }

        /// <summary>Fecha en que se marcó como recibida. Null si aún no se recibe.</summary>
        public DateTime? ReceivedAt { get; set; }
    }

    public static class ExpensePaymentTypes
    {
        public const string Cash = "Cash";
        public const string Installments = "Installments";
    }
}
