using BotSharp.Abstraction.Infrastructures;
using Medallion.Threading.Redis;
using StackExchange.Redis;

namespace BotSharp.Core.Infrastructures;

public class DistributedLocker : IDistributedLocker
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    public DistributedLocker(
        IServiceProvider services,
        ILogger<DistributedLocker> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<bool> LockAsync(string resource, Func<Task> action, int timeoutInSeconds = 30, int acquireTimeoutInSeconds = default)
    {
        var timeout = TimeSpan.FromSeconds(timeoutInSeconds);
        var acquireTimeout = TimeSpan.FromSeconds(acquireTimeoutInSeconds);

        var redis = _services.GetService<IConnectionMultiplexer>();
        if (redis == null)
        {
#if !DEBUG
            _logger.LogInformation($"The Redis server is experiencing issues and is not functioning as expected.");
#endif
            await action();
            return true;
        }

        var @lock = new RedisDistributedLock(resource, redis.GetDatabase(),option => option.Expiry(timeout));
        await using (var handle = await @lock.TryAcquireAsync(acquireTimeout))
        {
            if (handle == null) 
            {
                _logger.LogWarning($"Acquire lock for {resource} failed due to after {acquireTimeout}s timeout.");
                return false;
            }
            
            await action();
            return true;
        }
    }

    public bool Lock(string resource, Action action, int timeoutInSeconds = 30, int acquireTimeoutInSeconds = default)
    {
        var timeout = TimeSpan.FromSeconds(timeoutInSeconds);
        var acquireTimeout = TimeSpan.FromSeconds(acquireTimeoutInSeconds);

        var redis = _services.GetRequiredService<IConnectionMultiplexer>();
        if (redis == null)
        {
#if !DEBUG
            _logger.LogWarning($"The Redis server is experiencing issues and is not functioning as expected.");
#endif
            action();
            return false;
        }

        var @lock = new RedisDistributedLock(resource, redis.GetDatabase(), option => option.Expiry(timeout));
        using (var handle = @lock.TryAcquire(acquireTimeout))
        {
            if (handle == null)
            {
                _logger.LogWarning($"Acquire lock for {resource} failed due to after {acquireTimeout}s timeout.");
                return false;
            }
            else
            {
                action();
                return true;
            }
        }
    }
}
