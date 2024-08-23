using BotSharp.Abstraction.Infrastructures;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace BotSharp.Core.Infrastructures;

public class CacheService : ICacheService
{
    private readonly BotSharpDatabaseSettings _settings;
    private static ConnectionMultiplexer redis = null!;

    public CacheService(BotSharpDatabaseSettings settings)
    {
        _settings = settings;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
#if DEBUG
        return default;
#endif
        if (string.IsNullOrEmpty(_settings.Redis))
        {
            return default;
        }

        if (redis == null)
        {
            redis = ConnectionMultiplexer.Connect(_settings.Redis);
        }

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
#if DEBUG
        return default;
#endif
        if (string.IsNullOrEmpty(_settings.Redis))
        {
            return default;
        }

        if (redis == null)
        {
            redis = ConnectionMultiplexer.Connect(_settings.Redis);
        }

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
#if DEBUG
        return;
#endif
        if (string.IsNullOrEmpty(_settings.Redis))
        {
            return;
        }

        var db = redis.GetDatabase();
        await db.StringSetAsync(key, JsonConvert.SerializeObject(value), expiry);
    }
}
