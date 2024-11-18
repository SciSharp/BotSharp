using StackExchange.Redis;

namespace BotSharp.Core.Infrastructures.Events;

public class RedisPublisher : IEventPublisher
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ISubscriber _subscriber;
    private readonly ILogger _logger;

    public RedisPublisher(IConnectionMultiplexer redis, ILogger<RedisPublisher> logger)
    {
        _redis = redis;
        _logger = logger;
        _subscriber = _redis.GetSubscriber();
    }

    public async Task BroadcastAsync(string channel, string message)
    {
        await _subscriber.PublishAsync(channel, message);
    }

    public async Task PublishAsync(string channel, string message)
    {
        var db = _redis.GetDatabase();
        // Add a message to the stream, keeping only the latest 1 million messages
        await db.StreamAddAsync(channel, "message", message, 
            maxLength: 1000 * 10000);

        _logger.LogInformation($"Published message {channel} {message}");
    }

    public async Task ReDispatchAsync(string channel, int count = 10, string order = "asc")
    {
        var db = _redis.GetDatabase();

        var entries = await db.StreamRangeAsync(channel, "-", "+", count: count, messageOrder: order == "asc" ? Order.Ascending : Order.Descending);
        foreach (var entry in entries)
        {
            _logger.LogInformation($"Fetched message: {channel} {entry.Values[0].Value} ({entry.Id})");

            try
            {
                var messageId = await db.StreamAddAsync(channel, "message", entry.Values[0].Value);

                _logger.LogWarning($"ReDispatched message: {channel} {entry.Values[0].Value} ({messageId})");

                // Optionally delete the message to save space
                await db.StreamDeleteAsync(channel, [entry.Id]);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing message: {ex.Message}, event id: {channel} {entry.Id}");
            }
        }
    }
}
