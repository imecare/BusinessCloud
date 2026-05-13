using BusinessCloud.Application.Common.Interfaces;

namespace BusinessCloud.Infrastructure.Common.Services;

public class NoOpCacheService : ICacheService
{
    public Task<T?> GetAsync<T>(string key) => Task.FromResult(default(T));

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) => Task.CompletedTask;

    public Task RemoveAsync(string key) => Task.CompletedTask;
}
