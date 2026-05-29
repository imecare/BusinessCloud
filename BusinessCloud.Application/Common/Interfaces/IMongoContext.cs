using BusinessCloud.Application.Common.Dtos;
using BusinessCloud.Application.Payments.Queries.GetCustomerHistory;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessCloud.Application.Common.Interfaces;

public interface IMongoContext
{
    Task InsertAuditLogAsync(object logEntry, CancellationToken cancellationToken);
    Task UpdateCustomerReadModelAsync(int saleId, decimal amount, string reference, CancellationToken ct);
    Task<CustomerHistoryDto?> GetCustomerHistoryAsync(int saleId, CancellationToken ct);
    Task<List<CustomerHistoryDto>> GetCustomerHistoryByPhoneAsync(string tenantId, string customerPhone, CancellationToken ct);
    Task<List<AuditLogEntry>> GetAuditLogsBySaleIdAsync(int saleId, CancellationToken cancellationToken);

}

