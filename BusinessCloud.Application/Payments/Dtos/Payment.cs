using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessCloud.Application.Payments.Dtos
{
    public class RegisterPaymentRequest
    {
        public int SaleId { get; set; }
        public decimal Amount { get; set; }
        public string? PaymentMethod { get; set; }
    }

    public class PaymentResponse
    {
        public int PaymentId { get; set; }
        public decimal NewBalance { get; set; }
    }
}
