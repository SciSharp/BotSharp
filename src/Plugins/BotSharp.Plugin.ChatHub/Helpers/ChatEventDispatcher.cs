using System.Collections.Concurrent;
using System.Threading;

namespace BotSharp.Plugin.ChatHub.Helpers;

/// <summary>
/// Hands chat events to SignalR without ever making the caller wait for the network.
/// </summary>
/// <remarks>
/// <para>
/// WHY THIS EXISTS. Sending an event used to be a plain <c>await</c> on
/// <c>IClientProxy.SendAsync</c>, and <see cref="Observers.ChatHubObserver"/> reached it through
/// <c>GetAwaiter().GetResult()</c> — a synchronous, unbounded, uncancellable wait on a socket.
/// That observer runs on whatever thread pushed to the MessageHub, and for a tool's
/// <c>notifications/progress</c> that thread is the MCP client's own read loop (McpToolExecutor
/// supplies the progress reporter, and the MCP SDK invokes it inline on the loop). So one client
/// that had stopped reading — a dead browser tab, a socket a proxy had dropped without a FIN —
/// stopped the read loop, and the tool call whose progress was being relayed never saw its own
/// response: it neither returned nor threw, not even at the HttpClient timeout, because that
/// continuation needs the loop that is stalled. A ten-minute browser task finished perfectly and
/// the agent's turn was already dead (OneFlow, 2026-09-15).
/// </para>
/// <para>
/// Since <c>Subject.Synchronize</c> serialises every push, one such send also held up chat events
/// for every other conversation in the process.
/// </para>
/// <para>
/// WHAT THIS DOES. <see cref="Enqueue"/> is O(1) and returns at once; delivery happens on the
/// thread pool. Three properties matter and each answers a way the naive fix goes wrong:
/// </para>
/// <list type="bullet">
/// <item>ORDER IS KEPT, per target. Indications are a narration — "Now on plan step 2.1", then
/// "Clicking SEND" — and the one that arrives last is the one left on screen, so delivering them
/// concurrently would show them out of order. Each target has its own chain and nothing overtakes
/// within it. Two targets never wait for each other, which is the property the old code lacked.</item>
/// <item>A WEDGED SEND CANNOT OWN ITS CHAIN FOREVER. Each delivery carries
/// <see cref="_sendTimeout"/>, so a socket that will never drain costs that conversation one
/// timeout rather than everything queued behind it for the life of the process. Nothing blocks
/// while that timer runs — it only bounds a task.</item>
/// <item>THE QUEUE IS BOUNDED. A client that stops reading entirely would otherwise accumulate one
/// entry per event for as long as the conversation lives. Past <see cref="_maxQueued"/> the event
/// is dropped and said so in the log; by then that browser is not showing anything anyway, and the
/// conversation itself is persisted elsewhere — SignalR is the live view, not the record.</item>
/// </list>
/// <para>
/// Nothing here may throw into a caller: a send that fails is logged and the chain moves on. The
/// lanes clean themselves up — the last delivery for a target removes it — so an idle process
/// holds no entry per conversation it has ever seen.
/// </para>
/// </remarks>
public sealed class ChatEventDispatcher
{
    /// <summary>How long one delivery may take before it is abandoned. See the remarks.</summary>
    public static readonly TimeSpan DefaultSendTimeout = TimeSpan.FromSeconds(30);

    /// <summary>How many events may be waiting for one target before further ones are dropped.</summary>
    public const int DefaultMaxQueued = 256;

    private readonly ConcurrentDictionary<string, Lane> _lanes = new();
    private readonly TimeSpan _sendTimeout;
    private readonly int _maxQueued;

    public ChatEventDispatcher() : this(DefaultSendTimeout, DefaultMaxQueued)
    {
    }

    /// <summary>Overridable for tests, which cannot wait out the real timeout.</summary>
    public ChatEventDispatcher(TimeSpan sendTimeout, int maxQueued)
    {
        _sendTimeout = sendTimeout;
        _maxQueued = maxQueued;
    }

    /// <summary>
    /// Queue one delivery for <paramref name="target"/> and return. Never blocks, never throws.
    /// <paramref name="send"/> runs on the thread pool, after everything already queued for the
    /// same target and never concurrently with it.
    /// </summary>
    /// <param name="target">
    /// What the event is addressed to — the group or the user, prefixed with which — so the two
    /// dispatch modes cannot share a chain.
    /// </param>
    /// <param name="description">Named in the log if the delivery fails or is dropped.</param>
    public void Enqueue(string target, Func<CancellationToken, Task> send, ILogger logger, string description)
    {
        while (true)
        {
            var lane = _lanes.GetOrAdd(target, _ => new Lane());

            lock (lane.Gate)
            {
                // The lane retired between GetOrAdd and this lock — its last delivery finished.
                // Drop it (a no-op if the retiring side already did) and take a fresh one.
                if (lane.Retired)
                {
                    _lanes.TryRemove(new KeyValuePair<string, Lane>(target, lane));
                    continue;
                }

                if (lane.Pending >= _maxQueued)
                {
                    logger.LogWarning(
                        $"Dropped chat event '{description}' for {target}: {lane.Pending} already waiting, so the client is not reading.");
                    return;
                }

                lane.Pending++;
                lane.Tail = Deliver(lane.Tail, target, lane, send, logger, description);
            }

            return;
        }
    }

    /// <summary>
    /// The number of targets currently holding a queue. For tests and diagnostics: a healthy
    /// process settles back to zero.
    /// </summary>
    public int ActiveTargets => _lanes.Count;

    private Task Deliver(Task tail, string target, Lane lane, Func<CancellationToken, Task> send, ILogger logger, string description)
    {
        // TaskScheduler.Default, and no ExecuteSynchronously: a tail that is already complete must
        // still be continued on the thread pool. Inlining it here would run the send on the caller
        // — which is the thread this class exists to keep free.
        return tail.ContinueWith(async _ =>
        {
            try
            {
                using var cts = new CancellationTokenSource(_sendTimeout);
                await send(cts.Token);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"Failed to send chat event '{description}' to {target}");
            }
            finally
            {
                lock (lane.Gate)
                {
                    if (--lane.Pending == 0)
                    {
                        // Nothing left for this target. Retire under the same lock an enqueue takes,
                        // so a writer either gets in before this and keeps the lane alive, or sees
                        // Retired and starts a new one. It cannot land on a lane nothing will drain.
                        lane.Retired = true;
                        _lanes.TryRemove(new KeyValuePair<string, Lane>(target, lane));
                    }
                }
            }
        }, CancellationToken.None, TaskContinuationOptions.DenyChildAttach, TaskScheduler.Default).Unwrap();
    }

    private sealed class Lane
    {
        public readonly object Gate = new();
        public Task Tail = Task.CompletedTask;
        public int Pending;
        public bool Retired;
    }
}
