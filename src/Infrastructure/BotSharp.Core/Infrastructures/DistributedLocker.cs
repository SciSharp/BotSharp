using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;

namespace BotSharp.Core.Infrastructures;

public class DistributedLocker
{
    private readonly BotSharpDatabaseSettings _settings;
    private readonly RedLockFactory _lockFactory;

    public DistributedLocker(/*BotSharpDatabaseSettings settings*/)
    {
        // _settings = settings;

        var multiplexers = new List<RedLockMultiplexer>();
        foreach (var x in "".Split(';'))
        {
            var option = new ConfigurationOptions
            {
                AbortOnConnectFail = false,
                EndPoints = { x }
            };
            var _connMuliplexer = ConnectionMultiplexer.Connect(option);
            multiplexers.Add(_connMuliplexer);
        }

        _lockFactory = RedLockFactory.Create(multiplexers);
    }

    public async Task Lock(string resource, Func<Task> action)
    {
        var expiry = TimeSpan.FromSeconds(60);
        var wait = TimeSpan.FromSeconds(30);
        var retry = TimeSpan.FromSeconds(3);

        await using (var redLock = await _lockFactory.CreateLockAsync(resource, expiry, wait, retry))
        {
            if (redLock.IsAcquired)
            {
                await action();
            }
            else
            {
                Console.WriteLine($"Acquire locak failed due to {resource} after {wait}s timeout.");
            }
        }
    }
}
