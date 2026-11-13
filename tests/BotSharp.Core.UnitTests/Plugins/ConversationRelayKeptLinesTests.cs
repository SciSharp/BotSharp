using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Plugin.Twilio.Models;
using BotSharp.Plugin.Twilio.Services;
using BotSharp.Plugin.Twilio.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BotSharp.Core.UnitTests.Plugins;

/// <summary>
/// A ConversationRelay session keeps every line it says on its own in the conversation history, labeled with what it
/// was, so the history shows what the caller heard: the apology of a failed turn and the fillers said before it, idle
/// prompts and their goodbye, and a line said again after a cut-in. A line that never went out is not kept, a turn's
/// fillers are kept only once, and the lines said as the session hangs up play to the end.
/// </summary>
public class ConversationRelayKeptLinesTests
{
    private const string AgentId = "relay-agent";
    private const string Filler = "One moment.";
    private const string Apology = "Sorry, we hit a snag.";
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(15);

    [Fact]
    public async Task A_failed_turn_keeps_its_fillers_then_the_apology()
    {
        var call = Call.Start(Settings(), async (self, callbacks) =>
        {
            await WaitUntil(() => self.Socket.SentTexts().Contains(Filler));
            throw new InvalidOperationException("The model is down.");
        });

        await WaitUntil(() => call.Kept.Count == 2);
        await call.HangUpAsync();

        Assert.Equal(new[] { Filler, Apology }, call.Kept.Select(line => line.Text));
        Assert.Equal(new[] { ConversationRelayLabels.Filler, ConversationRelayLabels.Apology }, call.Kept.Select(line => line.Label));
        Assert.True(call.Kept.First().SaidUtc <= call.Kept.Last().SaidUtc);
        Assert.Equal(new[] { Filler, Apology }, call.Socket.SentTexts());
        Assert.True(call.Socket.SentEnd);
        // The call ends after the apology: the caller can't cut it off, a filler is left to the call's setting
        Assert.Equal(new bool?[] { null, false }, call.Socket.SentInterruptible());
    }

    [Fact]
    public async Task A_timed_out_turn_leaves_no_filler_to_keep_again_when_it_finally_replies()
    {
        var lateTake = new TaskCompletionSource<List<(string Text, DateTime SaidUtc)>>();
        var call = Call.Start(Settings(relay => relay.AiResponseTimeoutSeconds = 1), async (self, callbacks) =>
        {
            await WaitUntil(() => self.Socket.SentTexts().Contains(Filler));
            await WaitUntil(() => self.Kept.Count == 2);
            // Where TwilioConversationProcessor takes them as it stores the reply
            lateTake.SetResult(callbacks.TakeFillers());
            return new AssistantMessage { Content = "Here you go." };
        });

        await WaitUntil(() => call.Kept.Count == 2);
        Assert.Empty(await lateTake.Task.WaitAsync(Patience));
        await call.HangUpAsync();

        Assert.Equal(new[] { Filler, Apology }, call.Kept.Select(line => line.Text));
        Assert.DoesNotContain("Here you go.", call.Socket.SentTexts());
    }

    [Fact]
    public async Task Fillers_a_turn_kept_with_its_reply_are_not_kept_again()
    {
        List<(string Text, DateTime SaidUtc)>? taken = null;
        var call = Call.Start(Settings(), async (self, callbacks) =>
        {
            await WaitUntil(() => self.Socket.SentTexts().Contains(Filler));
            taken = callbacks.TakeFillers();
            return new AssistantMessage { Content = "Your request is in." };
        });

        await WaitUntil(() => call.Socket.SentTexts().Contains("Your request is in."));
        await call.HangUpAsync();

        Assert.Equal(new[] { Filler }, taken!.Select(line => line.Text));
        Assert.Empty(call.Kept);
    }

    [Fact]
    public async Task A_turn_that_stored_no_reply_leaves_its_fillers_to_the_session()
    {
        var call = Call.Start(Settings(), async (self, callbacks) =>
        {
            await WaitUntil(() => self.Socket.SentTexts().Contains(Filler));
            return null;
        });

        await WaitUntil(() => call.Kept.Count == 1);
        await call.HangUpAsync();

        Assert.Equal(new[] { (Filler, ConversationRelayLabels.Filler) }, call.Kept.Select(line => (line.Text, line.Label)));
    }

    [Fact]
    public async Task A_turn_failing_after_the_caller_hung_up_keeps_its_fillers_but_no_apology()
    {
        var fail = new TaskCompletionSource();
        var call = Call.Start(Settings(), async (self, callbacks) =>
        {
            await WaitUntil(() => self.Socket.SentTexts().Contains(Filler));
            await fail.Task;
            throw new InvalidOperationException("The model is down.");
        });

        await WaitUntil(() => call.Socket.SentTexts().Contains(Filler));
        call.Socket.Close();
        // The session marks the call ended as soon as it reads the close
        await Task.Delay(TimeSpan.FromMilliseconds(300));
        fail.SetResult();
        await call.Run.WaitAsync(Patience);

        Assert.Equal(new[] { Filler }, call.Kept.Select(line => line.Text));
        Assert.DoesNotContain(Apology, call.Socket.SentTexts());
    }

    [Fact]
    public async Task A_line_that_cannot_be_kept_does_not_stop_the_hang_up()
    {
        var call = Call.Start(Settings(), (_, _) => throw new InvalidOperationException("The model is down."),
            keep: (_, _, _) => throw new InvalidOperationException("The storage is down."));

        await WaitUntil(() => call.Socket.SentEnd);
        await call.HangUpAsync();

        Assert.Equal(new[] { Apology }, call.Socket.SentTexts());
    }

    [Fact]
    public async Task Idle_prompts_and_the_goodbye_are_kept_as_said()
    {
        var call = Call.Start(Settings(relay =>
            {
                relay.ResponseWatchdogIntervals = [];
                relay.IdleAsksAgain = true;
                relay.IdleTimeouts = [0.1];
                relay.MaxIdlePrompts = 2;
            }),
            (_, _) => Task.FromResult<AssistantMessage?>(new AssistantMessage { Content = "Which unit?" }));

        await WaitUntil(() => call.Socket.SentEnd);
        await WaitUntil(() => call.Kept.Count == 3);
        await call.HangUpAsync();

        // The reply is the turn's to keep; the prompts are kept with their text, not their names
        Assert.Equal(new[] { "Pardon? Which unit?", "Hello?", "Bye now." }, call.Kept.Select(line => line.Text));
        Assert.Equal(new[] { ConversationRelayLabels.IdlePrompt, ConversationRelayLabels.IdlePrompt, ConversationRelayLabels.IdleGoodbye },
            call.Kept.Select(line => line.Label));
        Assert.Equal(new[] { "Which unit?", "Pardon? Which unit?", "Hello?", "Bye now." }, call.Socket.SentTexts());
        // A prompt waits for the caller to speak, so they can cut it off; the goodbye plays to the end
        Assert.Equal(new bool?[] { null, null, null, false }, call.Socket.SentInterruptible());
    }

    [Fact]
    public async Task A_say_it_again_prompt_is_kept_with_its_text()
    {
        var call = Call.Start(Settings(relay =>
            {
                relay.ResponseWatchdogIntervals = [];
                relay.IdleTimeouts = [0.1];
                relay.MaxIdlePrompts = 1;
            }),
            (_, _) => Task.FromResult<AssistantMessage?>(null), prompt: null);

        await WaitUntil(() => call.Socket.SentEnd);
        await call.HangUpAsync();

        // Without IdleAsksAgain the hang-up says nothing
        Assert.Equal(new[] { ("Say that again?", ConversationRelayLabels.IdlePrompt) }, call.Kept.Select(line => (line.Text, line.Label)));
        Assert.Equal(new[] { "Say that again?" }, call.Socket.SentTexts());
    }

    [Fact]
    public async Task A_line_said_again_after_a_cut_in_is_kept()
    {
        const string line = "Is the address right?";
        var call = Call.Start(Settings(relay =>
            {
                relay.ResponseWatchdogIntervals = [];
                relay.ResumeAfterCutInSeconds = 0.2;
            }),
            (_, _) => Task.FromResult<AssistantMessage?>(new AssistantMessage { Content = line }));

        await WaitUntil(() => call.Socket.SentTexts().Contains(line));
        call.Socket.Receive(new { type = "interrupt", utteranceUntilInterrupt = "Is the", durationUntilInterruptMs = "400" });

        await WaitUntil(() => call.Kept.Count == 1);
        await call.HangUpAsync();

        Assert.Equal(new[] { (line, ConversationRelayLabels.SaidAgain) }, call.Kept.Select(kept => (kept.Text, kept.Label)));
        Assert.Equal(new[] { line, line }, call.Socket.SentTexts());
        Assert.Equal(new bool?[] { null, null }, call.Socket.SentInterruptible());
    }

    [Fact]
    public async Task Kept_lines_are_the_agent_s_out_of_its_context_at_the_time_they_were_said()
    {
        var stored = new List<RoleDialogModel>();
        var storage = new Mock<IConversationStorage>();
        storage.Setup(s => s.Append("conv-1", It.IsAny<IEnumerable<RoleDialogModel>>()))
            .Callback<string, IEnumerable<RoleDialogModel>>((_, dialogs) => stored.AddRange(dialogs))
            .Returns(Task.CompletedTask);
        var sp = new ServiceCollection().AddLogging().AddSingleton(storage.Object).BuildServiceProvider();
        var said = new DateTime(2026, 11, 13, 1, 31, 0, DateTimeKind.Utc);

        await TwilioConversationProcessor.KeepSaidLines(sp, AgentId, "conv-1", messageId: null,
            ConversationRelayLabels.IdlePrompt, [("Hello?", said), ("Say that again?", said.AddSeconds(2))]);
        await TwilioConversationProcessor.KeepSaidLines(sp, AgentId, "conv-1", messageId: "msg-1",
            ConversationRelayLabels.Filler, [(Filler, said)]);

        Assert.Equal(new[] { "Hello?", "Say that again?", Filler }, stored.Select(d => d.Content));
        Assert.Equal(new[] { ConversationRelayLabels.IdlePrompt, ConversationRelayLabels.IdlePrompt, ConversationRelayLabels.Filler },
            stored.Select(d => d.MessageLabel));
        Assert.All(stored, d =>
        {
            Assert.Equal(AgentRole.Assistant, d.Role);
            Assert.True(d.ExcludeFromContext);
            Assert.Equal(AgentId, d.CurrentAgentId);
        });
        Assert.Equal(new[] { said, said.AddSeconds(2), said }, stored.Select(d => d.CreatedAt));
        // A line of the session's own is a message of its own; a filler goes with the caller message it was said for
        Assert.NotEqual(stored[0].MessageId, stored[1].MessageId);
        Assert.Equal("msg-1", stored[2].MessageId);
    }

    [Fact]
    public async Task A_storage_failure_loses_only_the_record()
    {
        var storage = new Mock<IConversationStorage>();
        storage.Setup(s => s.Append(It.IsAny<string>(), It.IsAny<IEnumerable<RoleDialogModel>>()))
            .ThrowsAsync(new InvalidOperationException("The storage is down."));
        var sp = new ServiceCollection().AddLogging().AddSingleton(storage.Object).BuildServiceProvider();

        await TwilioConversationProcessor.KeepSaidLines(sp, AgentId, "conv-1", messageId: null, ConversationRelayLabels.Apology,
            [(Apology, DateTime.UtcNow)]);

        storage.Verify(s => s.Append("conv-1", It.IsAny<IEnumerable<RoleDialogModel>>()), Times.Once);
    }

    private static TwilioSetting Settings(Action<ConversationRelaySetting>? configure = null)
    {
        var relay = new ConversationRelaySetting
        {
            Enabled = true,
            ResponseWatchdogIntervals = [0.2],
            ResponseWatchdogAgentIds = [AgentId],
            ResponseWatchdogPrompts = ["hold-on-1"]
        };
        configure?.Invoke(relay);
        return new TwilioSetting
        {
            PromptTexts = new Dictionary<string, string>
            {
                ["hold-on-1"] = Filler,
                ["error"] = Apology,
                ["say-it-again-question"] = "Pardon? {0}",
                ["still-there"] = "Hello?",
                ["idle-goodbye"] = "Bye now.",
                ["say-it-again-1"] = "Say that again?",
                ["say-it-again-2"] = "Say that again?",
                ["say-it-again-3"] = "Say that again?",
                ["say-it-again-4"] = "Say that again?"
            },
            ConversationRelay = relay
        };
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        var giveUp = DateTime.UtcNow + Patience;
        while (!condition())
        {
            if (DateTime.UtcNow > giveUp)
            {
                throw new TimeoutException("The call never got there.");
            }
            await Task.Delay(TimeSpan.FromMilliseconds(20));
        }
    }

    /// <summary>
    /// A call on a session, with the lines it kept
    /// </summary>
    private sealed class Call
    {
        public FakeWebSocket Socket { get; } = new();
        public ConcurrentQueue<(string Text, string Label, DateTime SaidUtc)> Kept { get; } = new();
        public Task Run { get; private set; } = Task.CompletedTask;

        /// <param name="prompt">What the caller says first; null says nothing</param>
        public static Call Start(TwilioSetting settings, Func<Call, AgentTurnCallbacks, Task<AssistantMessage?>> turn,
            Func<string, string, DateTime, Task>? keep = null, string? prompt = "My sink is leaking")
        {
            var call = new Call();
            var session = new ConversationRelaySession(call.Socket, AgentId, "conv-1", settings,
                (_, callbacks) => turn(call, callbacks), NullLogger.Instance,
                keep ?? ((text, label, saidUtc) =>
                {
                    call.Kept.Enqueue((text, label, saidUtc));
                    return Task.CompletedTask;
                }));
            call.Run = session.RunAsync(CancellationToken.None);
            call.Socket.Receive(new { type = "setup", callSid = "CA1", from = "+15550100", to = "+15550199", direction = "inbound" });
            if (prompt != null)
            {
                call.Socket.Receive(new { type = "prompt", voicePrompt = prompt, last = true });
            }
            return call;
        }

        public async Task HangUpAsync()
        {
            Socket.Close();
            await Run.WaitAsync(Patience);
        }
    }

    private sealed class FakeWebSocket : WebSocket
    {
        private readonly Channel<byte[]?> _incoming = Channel.CreateUnbounded<byte[]?>();
        private readonly ConcurrentQueue<string> _sent = new();
        private WebSocketState _state = WebSocketState.Open;

        public void Receive(object message) => _incoming.Writer.TryWrite(JsonSerializer.SerializeToUtf8Bytes(message));

        public void Close() => _incoming.Writer.TryWrite(null);

        /// <summary>
        /// The text said, in order: each text frame's token, without the empty ones that close a streamed utterance
        /// </summary>
        public List<string> SentTexts()
        {
            return TextFrames().Select(frame => frame.GetProperty("token").GetString()!).ToList();
        }

        /// <summary>
        /// Each text said's interruptible flag, in order: null when the frame leaves it to the call's setting
        /// </summary>
        public List<bool?> SentInterruptible()
        {
            return TextFrames()
                .Select(frame => frame.TryGetProperty("interruptible", out var flag) ? flag.GetBoolean() : (bool?)null)
                .ToList();
        }

        private IEnumerable<JsonElement> TextFrames()
        {
            return Frames()
                .Where(frame => frame.GetProperty("type").GetString() == "text")
                .Where(frame => !string.IsNullOrEmpty(frame.GetProperty("token").GetString()));
        }

        public bool SentEnd => Frames().Any(frame => frame.GetProperty("type").GetString() == "end");

        private IEnumerable<JsonElement> Frames() => _sent.Select(json => JsonDocument.Parse(json).RootElement);

        public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            var data = await _incoming.Reader.ReadAsync(cancellationToken);
            if (data == null)
            {
                _state = WebSocketState.CloseReceived;
                return new WebSocketReceiveResult(0, WebSocketMessageType.Close, true);
            }
            data.CopyTo(buffer.Array!, buffer.Offset);
            return new WebSocketReceiveResult(data.Length, WebSocketMessageType.Text, true);
        }

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage,
            CancellationToken cancellationToken)
        {
            _sent.Enqueue(Encoding.UTF8.GetString(buffer.Array!, buffer.Offset, buffer.Count));
            return Task.CompletedTask;
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            _state = WebSocketState.Closed;
            return Task.CompletedTask;
        }

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            _state = WebSocketState.Closed;
            return Task.CompletedTask;
        }

        public override WebSocketCloseStatus? CloseStatus => _state == WebSocketState.Closed ? WebSocketCloseStatus.NormalClosure : null;
        public override string? CloseStatusDescription => null;
        public override WebSocketState State => _state;
        public override string? SubProtocol => null;
        public override void Abort() => _state = WebSocketState.Aborted;
        public override void Dispose() { }
    }
}
