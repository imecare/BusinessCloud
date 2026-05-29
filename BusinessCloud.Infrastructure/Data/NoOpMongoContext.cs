using BusinessCloud.Application.Common.Dtos;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Payments.Queries.GetCustomerHistory;

namespace BusinessCloud.Infrastructure.Data;

public class NoOpMongoContext : IMongoContext
{
    public Task InsertAuditLogAsync(object logEntry, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task UpdateCustomerReadModelAsync(int saleId, decimal amount, string reference, CancellationToken ct) => Task.CompletedTask;

    public Task<CustomerHistoryDto?> GetCustomerHistoryAsync(int saleId, CancellationToken ct) => Task.FromResult<CustomerHistoryDto?>(null);

    public Task<List<CustomerHistoryDto>> GetCustomerHistoryByPhoneAsync(string tenantId, string customerPhone, CancellationToken ct) => Task.FromResult(new List<CustomerHistoryDto>());

    public Task<List<AuditLogEntry>> GetAuditLogsBySaleIdAsync(int saleId, CancellationToken cancellationToken) => Task.FromResult(new List<AuditLogEntry>());
}
