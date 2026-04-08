//using BusinessCloud.Application.Common.Interfaces;
//using Microsoft.EntityFrameworkCore.Storage;
//using StackExchange.Redis;
//using System.Text.Json;

//namespace BusinessCloud.Application.Payments.Services;

//public class RedisCacheService : ICacheService
//{
//    private readonly StackExchange.Redis.IDatabase _db;

//    public RedisCacheService(IConnectionMultiplexer redis)
//    {
//        _db = redis.GetDatabase();
//    }

//    public async Task<T?> GetAsync<T>(string key)
//    {
//        var value = await _db.StringGetAsync(key);
//        return value.HasValue ? JsonSerializer.Deserialize<T>(value!) : default;
//    }

//    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
//    {
//        var options = expiration ?? TimeSpan.FromMinutes(60); // Cache por 1 hora por defecto
//        await _db.StringSetAsync(key, JsonSerializer.Serialize(value), options);
//    }

//    public async Task RemoveAsync(string key) => await _db.KeyDeleteAsync(key);
//}