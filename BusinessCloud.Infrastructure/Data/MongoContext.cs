using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Payments.Queries.GetCustomerHistory;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessCloud.Infrastructure.Data;

public class MongoContext : IMongoContext
{
    private readonly IMongoDatabase _db;
    public MongoContext(IMongoClient client) => _db = client.GetDatabase("PaymentsDB");

    public async Task InsertAuditLogAsync(object log, CancellationToken ct)
        => await _db.GetCollection<object>("AuditLogs").InsertOneAsync(log, null, ct);

    public async Task UpdateCustomerReadModelAsync(int saleId, decimal amount, string reference, CancellationToken ct)
    {
        var collection = _db.GetCollection<CustomerHistoryDto>("CustomerReadModel");
        var filter = Builders<CustomerHistoryDto>.Filter.Eq(x => x.SaleId, saleId);
        var update = Builders<CustomerHistoryDto>.Update
            .Push(x => x.Movements, new PaymentLineDto(0, DateTime.UtcNow, amount, reference))
            .Inc(x => x.RemainingBalance, -amount);

        await collection.UpdateOneAsync(filter, update, null, ct);
    }

    public async Task<CustomerHistoryDto?> GetCustomerHistoryAsync(int saleId, CancellationToken ct)
        => await _db.GetCollection<CustomerHistoryDto>("CustomerReadModel")
                    .Find(x => x.SaleId == saleId).FirstOrDefaultAsync(ct);

    // Implementación solicitada para consulta por tenant y teléfono
    public async Task<List<CustomerHistoryDto>> GetCustomerHistoryByPhoneAsync(string tenantId, string customerPhone, CancellationToken ct)
    {
        var collection = _db.GetCollection<CustomerHistoryDto>("CustomerReadModel");
        var filter = Builders<CustomerHistoryDto>.Filter.And(
            Builders<CustomerHistoryDto>.Filter.Eq("TenantId", tenantId),
            Builders<CustomerHistoryDto>.Filter.Eq("CustomerPhone", customerPhone)
        );

        return await collection.Find(filter)
                               .SortByDescending(x => x.Date)
                               .ToListAsync(ct);
    }
}