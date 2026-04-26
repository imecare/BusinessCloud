using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Payments.Queries.GetCustomerHistory;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessCloud.Infrastructure.Data;

public class MongoContext : IMongoContext
{
    private readonly IMongoDatabase _db;

    public MongoContext(IMongoClient client, IConfiguration configuration)
    {
        var databaseName = configuration["MongoDb:DatabaseName"] ?? "BusinessCloudDb";
        _db = client.GetDatabase(databaseName);
    }

    public async Task InsertAuditLogAsync(object logEntry, CancellationToken cancellationToken)
    {
        var collection = _db.GetCollection<BsonDocument>("AuditLogs");
        var document = logEntry.ToBsonDocument();
        await collection.InsertOneAsync(document, null, cancellationToken);
    }

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

    public async Task<List<dynamic>> GetAuditLogsBySaleIdAsync(int saleId, CancellationToken cancellationToken)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("SaleId", saleId);
        var documents = await _db.GetCollection<BsonDocument>("AuditLogs")
            .Find(filter)
            .ToListAsync(cancellationToken);

        return documents.Select(doc => BsonTypeMapper.MapToDotNetValue(doc)).ToList();
    }
}