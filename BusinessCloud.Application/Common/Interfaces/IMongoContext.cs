using BusinessCloud.Application.Payments.Queries.GetCustomerHistory;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessCloud.Application.Common.Interfaces;

public interface IMongoContext
{
    Task InsertAuditLogAsync(object log, CancellationToken ct);
    Task UpdateCustomerReadModelAsync(int saleId, decimal amount, string reference, CancellationToken ct);
    Task<CustomerHistoryDto?> GetCustomerHistoryAsync(int saleId, CancellationToken ct);

    // Nueva abstracción para consultar por tenant y teléfono.
    Task<List<CustomerHistoryDto>> GetCustomerHistoryByPhoneAsync(string tenantId, string customerPhone, CancellationToken ct);
}