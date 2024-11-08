using StackExchange.Redis;
using System.Threading.Channels;

namespace BotSharp.Core.Infrastructures.Events;

public class RedisSubscriber : IEventSubscriber
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ISubscriber _subscriber;
    private readonly ILogger _logger;

    public RedisSubscriber(IConnectionMultiplexer redis, ILogger<RedisSubscriber> logger)
    {
        _redis = redis;
        _logger = logger;
        _subscriber = _redis.GetSubscriber();
    }

    public async Task SubscribeAsync(string channel, Func<string, string, Task> received)
    {
        await _subscriber.SubscribeAsync(channel, async (ch, message) =>
        {
            _logger.LogInformation($"Received event from channel: {ch} message: {message}");
            await received(ch, message);
        });
    }

    public async Task SubscribeAsync(string channel, string group, Func<string, string, Task> received)
    {
        var db = _redis.GetDatabase();

        // Create the consumer group if it doesn't exist
        try
        {
            await db.StreamCreateConsumerGroupAsync(channel, group, StreamPosition.NewMessages, createStream: true);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
        {
            // Group already exists, ignore the error
            _logger.LogWarning($"Consumer group '{group}' already exists (caught exception).");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating consumer group: '{group}' {ex.Message}");
            throw;
        }

        while (true)
        {
            var entries = await db.StreamReadGroupAsync(channel, group, Environment.MachineName, count: 1);
            foreach (var entry in entries)
            {
                _logger.LogInformation($"Consumer {Environment.MachineName} received: {channel} {entry.Values[0].Value}");
                await db.StreamAcknowledgeAsync(channel, group, entry.Id);

                try
                {
                    await received(channel, entry.Values[0].Value);

                    // Optionally delete the message to save space
                    await db.StreamDeleteAsync(channel, [entry.Id]);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error processing message: {ex.Message}, event id: {channel} {entry.Id}");
                }
            }

            await Task.Delay(Random.Shared.Next(1, 11) * 100);
        }

    }
}
