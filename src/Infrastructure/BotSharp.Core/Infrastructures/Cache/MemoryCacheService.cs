using BotSharp.Abstraction.Infrastructures;
using Microsoft.Extensions.Caching.Memory;

namespace BotSharp.Core.Infrastructures;

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;

    public MemoryCacheService(IMemoryCache memoryCache)
    {
        this._cache = memoryCache;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        return (T?)(_cache.Get(key) ?? default(T));
    }

    public async Task<object> GetAsync(string key, Type type)
    {
        return _cache.Get(key) ?? default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry)
    {
        _cache.Set(key, value, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry
        });
    }

    public async Task RemoveAsync(string key)
    {
        _cache.Remove(key);
    }
}
