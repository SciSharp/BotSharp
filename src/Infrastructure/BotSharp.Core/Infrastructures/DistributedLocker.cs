using BotSharp.Abstraction.Infrastructures;
using Medallion.Threading.Redis;
using StackExchange.Redis;

namespace BotSharp.Core.Infrastructures;

public class DistributedLocker : IDistributedLocker
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    public DistributedLocker(IServiceProvider services, ILogger<DistributedLocker> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<bool> LockAsync(string resource, Func<Task> action, int timeoutInSeconds = 30)
    {
        var timeout = TimeSpan.FromSeconds(timeoutInSeconds);

        var redis = _services.GetRequiredService<IConnectionMultiplexer>();
        var @lock = new RedisDistributedLock(resource, redis.GetDatabase());
        await using (var handle = await @lock.TryAcquireAsync(timeout))
        {
            if (handle == null) 
            {
                _logger.LogWarning($"Acquire lock for {resource} failed due to after {timeout}s timeout.");
                return false;
            }
            
            await action();
            return true;
        }
    }

    public bool Lock(string resource, Action action, int timeoutInSeconds = 30)
    {
        var timeout = TimeSpan.FromSeconds(timeoutInSeconds);

        var redis = _services.GetRequiredService<IConnectionMultiplexer>();
        var @lock = new RedisDistributedLock(resource, redis.GetDatabase());
        using (var handle = @lock.TryAcquire(timeout))
        {
            if (handle == null)
            {
                _logger.LogWarning($"Acquire lock for {resource} failed due to after {timeout}s timeout.");
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
