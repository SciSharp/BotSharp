using BotSharp.Abstraction.Infrastructures;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace BotSharp.Core.Infrastructures;

public class RedisCacheService : ICacheService
{
    private IConnectionMultiplexer _redis = null!;

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
}
