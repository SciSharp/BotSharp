using BotSharp.Abstraction.Infrastructures;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace BotSharp.Core.Infrastructures;

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;

    public RedisCacheService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(key);

        if (value.HasValue)
        {
            return JsonConvert.DeserializeObject<T>(value);
        }

        return default;
    }

    public async Task<object> GetAsync(string key, Type type)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(key);

        if (value.HasValue)
        {
            return JsonConvert.DeserializeObject(value, type);
        }

        return default;
    }


    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry)
    {
        var db = _redis.GetDatabase();
        await db.StringSetAsync(key, JsonConvert.SerializeObject(value), expiry);
    }

    public async Task RemoveAsync(string key)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(key);
    }

    public async Task ClearCacheAsync(string prefix)
    {
        var db = _redis.GetDatabase();
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        const int pageSize = 1000;
        var keys = server.Keys(pattern: $"{prefix}*", pageSize: pageSize).ToList();

        for (int i = 0; i < keys.Count; i += pageSize)
        {
            var batch = keys.Skip(i).Take(pageSize).ToArray();
            await db.KeyDeleteAsync(batch);
        }
    }
}
