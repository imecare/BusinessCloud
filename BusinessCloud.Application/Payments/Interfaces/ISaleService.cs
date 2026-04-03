
using BusinessCloud.Application.Payments.Dtos;

namespace BusinessCloud.Application.Payments.Interfaces
{
    public interface ISaleService
    {
        Task<SaleResponse> CreateSaleAsync(CreateSaleRequest request);
        Task<IEnumerable<SaleResponse>> GetCustomerHistoryAsync(string rfc, string phone);
    }
}
