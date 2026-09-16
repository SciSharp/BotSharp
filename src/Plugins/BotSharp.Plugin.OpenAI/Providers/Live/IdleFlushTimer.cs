namespace BotSharp.Plugin.OpenAI.Providers.Live;

/// <summary>
/// Fires a callback once a stream of events has been quiet for a while. The Live endpoint
/// never marks the end of a turn, so audio and transcript boundaries are inferred from silence.
/// </summary>
internal sealed class IdleFlushTimer : IDisposable
{
    private readonly TimeSpan _idle;
    private readonly Func<Task> _onIdle;
    private readonly ILogger? _logger;
    private readonly string _name;
    private readonly object _lock = new();

    private Timer? _timer;
    private bool _disposed;

    public IdleFlushTimer(string name, TimeSpan idle, Func<Task> onIdle, ILogger? logger = null)
    {
        _name = name;
        _idle = idle;
        _onIdle = onIdle;
        _logger = logger;
    }

    /// <summary>
    /// Restarts the idle countdown. Call on every delta.
    /// </summary>
    public void Touch()
    {
        lock (_lock)
        {
            if (_disposed) return;

            _timer ??= new Timer(OnElapsed, null, Timeout.Infinite, Timeout.Infinite);
            _timer.Change(_idle, Timeout.InfiniteTimeSpan);
        }
    }

    /// <summary>
    /// Stops the countdown without invoking the callback, e.g. after an explicit flush.
    /// </summary>
    public void Cancel()
    {
        lock (_lock)
        {
            if (_disposed) return;
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }

    private void OnElapsed(object? state) => _ = FireAsync();

    private async Task FireAsync()
    {
        try
        {
            await _onIdle();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error while flushing idle {Name} buffer.", _name);
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;

            _disposed = true;
            _timer?.Dispose();
            _timer = null;
        }
    }
}
