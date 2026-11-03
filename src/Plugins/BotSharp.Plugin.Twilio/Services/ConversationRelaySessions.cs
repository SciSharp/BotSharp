using Task = System.Threading.Tasks.Task;

namespace BotSharp.Plugin.Twilio.Services;

/// <summary>
/// The ConversationRelay sessions running on this instance, by conversation, so the session-ended callback can let one
/// finish the turn it is on (and save the conversation) before telling the hooks the call is over
/// </summary>
public class ConversationRelaySessions
{
    // A reconnect can open a second session for a conversation while the first one still finishes its turn
    private readonly Dictionary<string, List<Task>> _running = new();

    /// <summary>
    /// Runs the session of <paramref name="conversationId"/>, known as running until it returns
    /// </summary>
    public async Task RunAsync(string conversationId, Func<Task> run)
    {
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_running)
        {
            if (!_running.TryGetValue(conversationId, out var sessions))
            {
                _running[conversationId] = sessions = [];
            }
            sessions.Add(done.Task);
        }

        try
        {
            await run();
        }
        finally
        {
            lock (_running)
            {
                var sessions = _running[conversationId];
                sessions.Remove(done.Task);
                if (sessions.Count == 0)
                {
                    _running.Remove(conversationId);
                }
            }
            done.SetResult();
        }
    }

    /// <summary>
    /// Waits up to <paramref name="timeout"/> for the sessions of <paramref name="conversationId"/> on this instance to
    /// end, and says whether they did. One held by another instance is not seen, so that is not waited for.
    /// </summary>
    public async Task<bool> WaitForAsync(string conversationId, TimeSpan timeout)
    {
        Task[] sessions;
        lock (_running)
        {
            sessions = _running.TryGetValue(conversationId, out var running) ? running.ToArray() : [];
        }

        if (sessions.Length == 0)
        {
            return true;
        }

        var ended = Task.WhenAll(sessions);
        return await Task.WhenAny(ended, Task.Delay(timeout)) == ended;
    }
}
