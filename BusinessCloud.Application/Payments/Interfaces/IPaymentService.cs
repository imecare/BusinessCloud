using BusinessCloud.Application.Payments.Dtos;

namespace BusinessCloud.Application.Payments.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentResponse> RegisterPaymentAsync(RegisterPaymentRequest request);
    }
}

