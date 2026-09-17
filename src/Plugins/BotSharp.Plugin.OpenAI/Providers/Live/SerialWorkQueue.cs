using System.Threading.Channels;

namespace BotSharp.Plugin.OpenAI.Providers.Live;

/// <summary>
/// Runs queued work on a single background worker, one item at a time and in the order it was
/// queued.
///
/// The Live socket has one consumer, and anything awaited on it stops the socket being read.
/// That is fatal on a full duplex call: the model keeps speaking while a tool runs, so audio
/// frames pile up unread and the caller hears a gap where the reply should be. Conversation
/// work is handed here instead, so the receive loop only ever parses an event and moves on.
///
/// Serial rather than fire and forget for two reasons: two tool calls must not interleave their
/// session updates, and the conversation state this work touches is not thread safe.
/// </summary>
internal sealed class SerialWorkQueue : IDisposable
{
    private readonly Channel<Func<Task>> _queue = Channel.CreateUnbounded<Func<Task>>(
        new UnboundedChannelOptions { SingleReader = true });

    /// <summary>
    /// The queue whose worker the current call is running on, if any. Flows into the work's own
    /// continuations, which is how <see cref="DrainAsync"/> spots being called from inside itself.
    /// </summary>
    private static readonly AsyncLocal<SerialWorkQueue?> _running = new();

    private readonly Task _worker;
    private readonly ILogger? _logger;
    private readonly string _name;

    public SerialWorkQueue(string name, ILogger? logger = null)
    {
        _name = name;
        _logger = logger;
        _worker = Task.Run(RunAsync);
    }

    /// <summary>
    /// Hands work to the worker and returns at once. Never throws: a queue that fails to accept
    /// work must not take the caller's receive loop down with it.
    /// </summary>
    public void Enqueue(Func<Task> work)
    {
        if (_queue.Writer.TryWrite(work)) return;

        // Only happens once the queue is closed, i.e. the session is already tearing down.
        _logger?.LogWarning("Dropped {Name} work: the queue is closed.", _name);
    }

    /// <summary>
    /// Stops accepting work and waits for what is already queued to finish. Called during
    /// teardown, where the final turns still have to reach storage before the session goes.
    /// </summary>
    public async Task DrainAsync()
    {
        _queue.Writer.TryComplete();

        // Queued work can end up here: a tool call runs on this worker, and handling it can
        // tear the session down - a reconnect, say. Waiting would be waiting on ourselves.
        // Closing the queue is still right; what is already in it runs as this worker unwinds.
        if (ReferenceEquals(_running.Value, this))
        {
            _logger?.LogDebug("Not waiting on the {Name} queue from inside its own worker.", _name);
            return;
        }

        try
        {
            await _worker;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to drain the {Name} queue.", _name);
        }
    }

    private async Task RunAsync()
    {
        _running.Value = this;

        await foreach (var work in _queue.Reader.ReadAllAsync())
        {
            try
            {
                await work();
            }
            catch (Exception ex)
            {
                // One failed item must not end the worker; the rest of the call still needs it.
                _logger?.LogError(ex, "Error while running queued {Name} work.", _name);
            }
        }
    }

    public void Dispose() => _queue.Writer.TryComplete();
}
