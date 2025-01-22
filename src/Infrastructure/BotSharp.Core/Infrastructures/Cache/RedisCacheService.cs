using BotSharp.Abstraction.Infrastructures;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace BotSharp.Core.Infrastructures;

public class RedisCacheService : ICacheService
{
    private readonly IServiceProvider _services;

    public RedisCacheService()
    {
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var redis = _services.GetService<IConnectionMultiplexer>();
        var db = redis.GetDatabase();
        var value = await db.StringGetAsync(key);

        if (value.HasValue)
        {
            return JsonConvert.DeserializeObject<T>(value);
        }

        return default;
    }

    public async Task<object> GetAsync(string key, Type type)
    {
        var redis = _services.GetService<IConnectionMultiplexer>();
        var db = redis.GetDatabase();
        var value = await db.StringGetAsync(key);

        if (value.HasValue)
        {
            return JsonConvert.DeserializeObject(value, type);
        }

        return default;
    }


    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry)
    {
        var redis = _services.GetService<IConnectionMultiplexer>();
        var db = redis.GetDatabase();
        await db.StringSetAsync(key, JsonConvert.SerializeObject(value), expiry);
    }

    public async Task RemoveAsync(string key)
    {
        var redis = _services.GetService<IConnectionMultiplexer>();
        var db = redis.GetDatabase();
        await db.KeyDeleteAsync(key);
    }

    public async Task ClearCacheAsync(string prefix)
    {
        var redis = _services.GetService<IConnectionMultiplexer>();
        var db = redis.GetDatabase();
        var server = redis.GetServer(redis.GetEndPoints().First());
        const int pageSize = 1000;
        var keys = server.Keys(pattern: $"{prefix}*", pageSize: pageSize).ToList();

        for (int i = 0; i < keys.Count; i += pageSize)
        {
            var batch = keys.Skip(i).Take(pageSize).ToArray();
            await db.KeyDeleteAsync(batch);
        }
    }
}
