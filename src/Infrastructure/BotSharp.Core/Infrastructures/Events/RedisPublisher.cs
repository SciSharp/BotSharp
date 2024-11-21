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

        if (CheckMessageExists(db, channel, "message", message))
        {
            _logger.LogError($"The message already exists {channel} {message}");
            return;
        }

        // Add a message to the stream, keeping only the latest 1 million messages
        var messageId = await db.StreamAddAsync(channel, 
            [
                new NameValueEntry("message", message),
                new NameValueEntry("timestamp", DateTime.UtcNow.ToString("o"))
            ],
            maxLength: 1000 * 10000);

        _logger.LogInformation($"Published message {channel} {message} ({messageId})");
    }

    private bool CheckMessageExists(IDatabase db, string streamName, string fieldName, string desiredValue)
    {
        // Define the range to fetch all messages
        RedisValue start = "-"; // Start from the smallest ID
        RedisValue end = "+";   // End at the largest ID
        int count = 10;        // Number of messages to retrieve

        // Fetch the latest 10 messages
        var streamEntries = db.StreamRange(streamName, start, end, count, Order.Descending);

        if (streamEntries.Length == 0)
        {
            return false;
        }

        // Check if any message contains the specific field value
        bool exists = streamEntries.Any(entry =>
        {
            // Each entry contains a collection of Name-Value pairs
            foreach (var nameValue in entry.Values)
            {
                if (nameValue.Name == fieldName && nameValue.Value == desiredValue)
                {
                    return true;
                }
            }
            return false;
        });

        return exists;
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
                _logger.LogError($"Error processing message: {ex.Message}, event id: {channel} {entry.Id}\r\n{ex}");
            }
        }
    }

    public async Task ReDispatchPendingAsync(string channel, string group, int count = 10)
    {
        var db = _redis.GetDatabase();

        // Step 1: Fetch pending messages using XPending
        try
        {
            var pendingInfo = await db.StreamPendingAsync(channel, group);

            if (pendingInfo.PendingMessageCount == 0)
            {
                Console.WriteLine("No pending messages found.");
                return;
            }

            // Step 2: Fetch pending message IDs
            List<StreamEntry> pendingMessages = new List<StreamEntry>();
            foreach (var consumer in pendingInfo.Consumers)
            {
                var pendingMessageIds = await db.StreamPendingMessagesAsync(channel, group, count: consumer.PendingMessageCount, consumerName: consumer.Name);
                var messageIds = pendingMessageIds.Select(x => x.MessageId).ToArray();
                // Step 3: Use XClaim to fetch the actual message
                var claimedMessages = await db.StreamClaimAsync(channel, group, consumer.Name, minIdleTimeInMs: 60 * 1000, messageIds);
                pendingMessages.AddRange(claimedMessages);

                await db.StreamAcknowledgeAsync(channel, group, messageIds);
            }

            // Step 4: Process the messages
            foreach (var message in pendingMessages)
            {
                /*if (message.IsNull)
                {
                    await db.StreamAcknowledgeAsync(channel, group, [message.Id]);
                }
                else
                {
                    var messageId = await db.StreamAddAsync(channel, "message", message.Values[0].Value);
                    _logger.LogWarning($"ReDispatched message: {channel} {message.Values[0].Value} ({messageId})");
                    await db.StreamDeleteAsync(channel, [messageId]);
                }*/
            }
        }
        catch (RedisException ex)
        {
            Console.WriteLine($"Redis error: {ex.Message}");
        }
    }
}
