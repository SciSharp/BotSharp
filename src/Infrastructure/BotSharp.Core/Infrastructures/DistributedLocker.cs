using Medallion.Threading.Redis;
using StackExchange.Redis;

namespace BotSharp.Core.Infrastructures;

public class DistributedLocker
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger _logger;

    public DistributedLocker(IConnectionMultiplexer redis, ILogger<DistributedLocker> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<T> Lock<T>(string resource, Func<Task<T>> action, int timeoutInSeconds = 30)
    {
        var timeout = TimeSpan.FromSeconds(timeoutInSeconds);

        var @lock = new RedisDistributedLock(resource, _redis.GetDatabase());
        await using (var handle = await @lock.TryAcquireAsync(timeout))
        {
            if (handle == null) 
            {
                _logger.LogWarning($"Acquire lock for {resource} failed due to after {timeout}s timeout.");
            }
            
            return await action();
        }
    }

    public bool Lock(string resource, Action action, int timeoutInSeconds = 30)
    {
        var timeout = TimeSpan.FromSeconds(timeoutInSeconds);

        var @lock = new RedisDistributedLock(resource, _redis.GetDatabase());
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
