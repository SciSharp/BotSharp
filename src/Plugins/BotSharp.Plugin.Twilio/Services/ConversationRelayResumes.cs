using StackExchange.Redis;
using System.Collections.Concurrent;
using Task = System.Threading.Tasks.Task;

namespace BotSharp.Plugin.Twilio.Services;

/// <summary>
/// What a session connected again in place of one whose playback was stuck starts with
/// (<see cref="Models.Relay.ConversationRelayResume"/>), from the stuck session to Twilio's callback asking what next
/// </summary>
/// <remarks>
/// Why kept here and not sent with the session's end: Twilio acts on an "end" message only once what is queued has played,
/// and a stuck playback never has, so the stuck session drops its socket instead, which carries nothing. The callback may
/// run on another instance: kept in Redis when there is one, in this instance otherwise.
/// </remarks>
public class ConversationRelayResumes
{
    // Long enough for Twilio's callback, which comes within seconds of the socket dropping
    private static readonly TimeSpan Kept = TimeSpan.FromMinutes(5);

    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, (string Json, DateTime UntilUtc)> _here = new();

    public ConversationRelayResumes(IServiceProvider services, ILogger<ConversationRelayResumes> logger)
    {
        _services = services;
        _logger = logger;
    }

    /// <summary>
    /// Keeps what the session taking the place of <paramref name="conversationId"/>'s stuck one starts with
    /// </summary>
    public async Task KeepAsync(string conversationId, string json)
    {
        if (_services.GetService<IConnectionMultiplexer>() is { } redis)
        {
            try
            {
                await redis.GetDatabase().StringSetAsync(Key(conversationId), json, Kept);
                return;
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(ex, $"Could not keep the resume of conversation {conversationId} in Redis; kept on this instance.");
            }
        }
        _here[conversationId] = (json, DateTime.UtcNow + Kept);
    }

    /// <summary>
    /// Takes what was kept for <paramref name="conversationId"/>, once: null when nothing was, or it is gone
    /// </summary>
    public async Task<string?> TakeAsync(string conversationId)
    {
        if (string.IsNullOrEmpty(conversationId))
        {
            return null;
        }

        if (_services.GetService<IConnectionMultiplexer>() is { } redis)
        {
            try
            {
                // Read and deleted in one transaction, so two callbacks at once (Twilio retrying one) can't both take it.
                // Not GETDEL, which needs Redis 6.2.
                var transaction = redis.GetDatabase().CreateTransaction();
                var kept = transaction.StringGetAsync(Key(conversationId));
                var deleted = transaction.KeyDeleteAsync(Key(conversationId));
                await transaction.ExecuteAsync();
                await deleted;
                var value = await kept;
                if (value.HasValue)
                {
                    return value.ToString();
                }
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(ex, $"Could not read the resume of conversation {conversationId} from Redis.");
            }
        }
        return _here.TryRemove(conversationId, out var here) && here.UntilUtc > DateTime.UtcNow ? here.Json : null;
    }

    private static string Key(string conversationId) => $"twilio:relay:resume:{conversationId}";
}
