using System.Diagnostics;
using BotSharp.Plugin.ChatHub.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BotSharp.Core.UnitTests.ChatHub;

/// <summary>
/// Chat events must never make their producer wait for a client.
///
/// The producer is often not a request thread: ChatHubObserver runs on whatever pushed to the
/// MessageHub, and for a tool's progress notifications that is the MCP client's own read loop. A
/// send that waited there stopped the loop, so the tool call being narrated never saw its own
/// response — it neither returned nor threw. These pin the properties that close that off.
/// </summary>
public class ChatEventDispatcherTests
{
    private static readonly TimeSpan ShortTimeout = TimeSpan.FromMilliseconds(300);

    private static ChatEventDispatcher Dispatcher(int maxQueued = 256)
        => new(ShortTimeout, maxQueued);

    private static void Enqueue(ChatEventDispatcher dispatcher, string target, Func<CancellationToken, Task> send)
        => dispatcher.Enqueue(target, send, NullLogger.Instance, "test event");

    [Fact]
    public async Task EnqueueDoesNotWaitForTheSend()
    {
        var dispatcher = Dispatcher();
        var stuck = new TaskCompletionSource();
        var entered = new TaskCompletionSource();

        var watch = Stopwatch.StartNew();
        Enqueue(dispatcher, "group:a", _ =>
        {
            entered.TrySetResult();
            return stuck.Task;
        });
        watch.Stop();

        Assert.True(watch.ElapsedMilliseconds < 200, $"Enqueue took {watch.ElapsedMilliseconds}ms — it waited for the send.");

        // And the send really did start, so the call above returned while it was in flight rather
        // than because nothing happened.
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        stuck.TrySetResult();
    }

    [Fact]
    public async Task OneStalledTargetDoesNotHoldUpAnother()
    {
        var dispatcher = Dispatcher();
        var stuck = new TaskCompletionSource();
        var delivered = new TaskCompletionSource();

        Enqueue(dispatcher, "group:stalled", _ => stuck.Task);
        Enqueue(dispatcher, "group:healthy", _ =>
        {
            delivered.TrySetResult();
            return Task.CompletedTask;
        });

        await delivered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        stuck.TrySetResult();
    }

    [Fact]
    public async Task DeliveriesToOneTargetKeepTheirOrder()
    {
        var dispatcher = Dispatcher();
        var order = new List<int>();
        var all = new TaskCompletionSource();
        const int count = 50;

        for (var i = 0; i < count; i++)
        {
            var n = i;
            Enqueue(dispatcher, "group:a", async _ =>
            {
                // Uneven work: concurrent delivery would reorder these, a serial chain cannot.
                await Task.Delay(n % 3);
                order.Add(n);
                if (order.Count == count) all.TrySetResult();
            });
        }

        await all.Task.WaitAsync(TimeSpan.FromSeconds(30));
        Assert.Equal(Enumerable.Range(0, count), order);
    }

    [Fact]
    public async Task AWedgedSendIsAbandonedSoTheNextOneRuns()
    {
        var dispatcher = Dispatcher();
        var cancelled = new TaskCompletionSource();
        var next = new TaskCompletionSource();

        // Never completes on its own; only the dispatcher's own timeout can end it.
        Enqueue(dispatcher, "group:a", async ct =>
        {
            try
            {
                await Task.Delay(Timeout.Infinite, ct);
            }
            catch (OperationCanceledException)
            {
                cancelled.TrySetResult();
                throw;
            }
        });
        Enqueue(dispatcher, "group:a", _ =>
        {
            next.TrySetResult();
            return Task.CompletedTask;
        });

        await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await next.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task AClientThatNeverReadsCannotQueueWithoutBound()
    {
        var dispatcher = Dispatcher(maxQueued: 4);
        var stuck = new TaskCompletionSource();
        var started = 0;

        for (var i = 0; i < 100; i++)
        {
            Enqueue(dispatcher, "group:a", _ =>
            {
                Interlocked.Increment(ref started);
                return stuck.Task;
            });
        }

        stuck.TrySetResult();
        await WaitUntil(() => dispatcher.ActiveTargets == 0, TimeSpan.FromSeconds(10));

        // The first is in flight and at most `maxQueued` are ever waiting, so nothing near 100
        // was accepted. The rest were dropped with a warning rather than held.
        Assert.InRange(Volatile.Read(ref started), 1, 8);
    }

    [Fact]
    public async Task AnIdleDispatcherHoldsNothing()
    {
        var dispatcher = Dispatcher();
        for (var i = 0; i < 20; i++)
        {
            Enqueue(dispatcher, $"group:{i}", _ => Task.CompletedTask);
        }

        await WaitUntil(() => dispatcher.ActiveTargets == 0, TimeSpan.FromSeconds(10));
        Assert.Equal(0, dispatcher.ActiveTargets);
    }

    private static async Task WaitUntil(Func<bool> condition, TimeSpan limit)
    {
        var watch = Stopwatch.StartNew();
        while (!condition() && watch.Elapsed < limit)
        {
            await Task.Delay(10);
        }
    }
}
