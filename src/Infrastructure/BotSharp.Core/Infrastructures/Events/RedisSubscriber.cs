using BotSharp.Abstraction.Infrastructures.Enums;
using StackExchange.Redis;

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

    public async Task SubscribeAsync(string channel, string group, bool priorityEnabled, Func<string, string, Task> received)
    {
        var db = _redis.GetDatabase();

        if (priorityEnabled)
        {
            await CreateConsumerGroup(db, $"{channel}-{EventPriority.Low}", group);
            await CreateConsumerGroup(db, $"{channel}-{EventPriority.Medium}", group);
            await CreateConsumerGroup(db, $"{channel}-{EventPriority.High}", group);
        }
        else
        {
            await CreateConsumerGroup(db, channel, group);
        }

        while (true)
        {
            await Task.Delay(100);

            if (priorityEnabled)
            {
                if (await HandleGroupMessage(db, $"{channel}-{EventPriority.High}", group, received) > 0)
                {
                    continue;
                }

                if (await HandleGroupMessage(db, $"{channel}-{EventPriority.Medium}", group, received) > 0)
                {
                    continue;
                }

                await HandleGroupMessage(db, $"{channel}-{EventPriority.Low}", group, received);
            }
            else
            {
                await HandleGroupMessage(db, channel, group, received);
            }
        }
    }

    private async Task<int> HandleGroupMessage(IDatabase db, string channel, string group, Func<string, string, Task> received)
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
                _logger.LogError($"Error processing message: {ex.Message}, event id: {channel} {entry.Id}\r\n{ex}");
            }
        }

        return entries.Length;
    }

    private async Task CreateConsumerGroup(IDatabase db, string channel, string group)
    {
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
    }
}
