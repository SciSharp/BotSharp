using BotSharp.Abstraction.Infrastructures;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace BotSharp.Core.Infrastructures;

public class MemoryCacheService : ICacheService
{
    private static readonly MemoryCache _cache = new MemoryCache(new OptionsWrapper<MemoryCacheOptions>(new MemoryCacheOptions()));

    public MemoryCacheService()
    {
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

    public async Task ClearCacheAsync(string prefix)
    {
        _cache.Compact(1.0);
    }
}
