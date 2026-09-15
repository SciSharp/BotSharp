using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Models;
using BotSharp.Abstraction.Utilities;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.MessageHub.Models;
using BotSharp.Abstraction.MessageHub.Services;
using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Rules;
using BotSharp.Abstraction.Rules.Options;
using BotSharp.Core.Rules.Engines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BotSharp.Core.UnitTests.Rules;

/// <summary>
/// Pins what a cancelled rule costs the rules behind it. The engine runs every rule in one loop, so the
/// two cancellations it can see - the caller giving up on the whole run, and one rule outstaying its own
/// limit - have to be told apart there, or they collapse into each other: a per-rule limit that ends the
/// run, or a cancelled run that carries on dispatching.
/// </summary>
public class RuleEngineCancellationTests
{
    private const string TriggerName = "stub_trigger";

    private static List<string> _log = [];

    private sealed class StubTrigger : IRuleTrigger
    {
        public string EntityType { get; set; } = "test";
        public string EntityId { get; set; } = "test";
        public string Name => TriggerName;
        public string Channel => "test";
    }

    /// <summary>
    /// Two agents, each subscribed to the trigger, dispatched in the order given.
    /// </summary>
    /// <param name="sendDuration">
    /// How long that agent's turn takes. The real SendMessage takes no cancellation token, so a slow turn
    /// here is uninterruptible in the test for the same reason it is in production - the limit can only be
    /// noticed once the turn is over.
    /// </param>
    /// <summary>
    /// Captures what the engine logged. The engine swallows a misbehaving rule's exception by design, so
    /// without this a broken test setup is indistinguishable from a rule that legitimately did nothing.
    /// </summary>
    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Messages.Add($"{logLevel}: {formatter(state, exception)}{(exception == null ? "" : " | " + exception)}");
    }

    private static (RuleEngine engine, List<string> sentToAgents) Build(
        Dictionary<string, TimeSpan>? sendDuration = null,
        Action<string>? onSendStarted = null)
    {
        var durations = sendDuration ?? [];
        var sentToAgents = new List<string>();

        var agents = new PagedItems<Agent>
        {
            Count = 2,
            Items =
            [
                new Agent { Id = "agent-1", Name = "agent-1", Disabled = false, Rules = [new AgentRule { TriggerName = TriggerName }] },
                new Agent { Id = "agent-2", Name = "agent-2", Disabled = false, Rules = [new AgentRule { TriggerName = TriggerName }] }
            ]
        };

        var agentService = new Mock<IAgentService>();
        agentService.Setup(x => x.GetAgents(It.IsAny<AgentFilter>())).ReturnsAsync(agents);

        var convService = new Mock<IConversationService>();
        convService.Setup(x => x.NewConversation(It.IsAny<Conversation>()))
            .ReturnsAsync((Conversation c) => new Conversation { Id = $"conv-{c.AgentId}", AgentId = c.AgentId });
        convService.Setup(x => x.SetConversationId(It.IsAny<string>(), It.IsAny<List<MessageState>>(), It.IsAny<bool>()))
            .Returns(Task.CompletedTask);
        convService.Setup(x => x.SaveStates()).Returns(Task.CompletedTask);
        convService.Setup(x => x.SendMessage(
                It.IsAny<string>(),
                It.IsAny<RoleDialogModel>(),
                It.IsAny<PostbackMessageModel?>(),
                It.IsAny<Func<RoleDialogModel, Task>>()))
            .Returns(async (string agentId, RoleDialogModel _, PostbackMessageModel? _, Func<RoleDialogModel, Task> _) =>
            {
                sentToAgents.Add(agentId);
                onSendStarted?.Invoke(agentId);

                if (durations.TryGetValue(agentId, out var duration))
                {
                    await Task.Delay(duration);
                }

                return true;
            });

        var observer = new Mock<IObserverService>();
        observer.Setup(x => x.SubscribeObservers<HubObserveData<RoleDialogModel>>(
                It.IsAny<string>(), It.IsAny<IEnumerable<string>?>(), It.IsAny<Dictionary<string, Func<HubObserveData<RoleDialogModel>, Task>>?>()))
            .Returns(Mock.Of<IDisposable>());

        var services = new ServiceCollection();
        services.AddSingleton(agentService.Object);
        services.AddSingleton(convService.Object);
        services.AddSingleton(observer.Object);

        var logger = new CapturingLogger<RuleEngine>();
        _log = logger.Messages;
        return (new RuleEngine(services.BuildServiceProvider(), logger), sentToAgents);
    }

    private static Task<IEnumerable<string>> Run(RuleEngine engine, RuleTriggerOptions options, CancellationToken token = default)
        => engine.Triggered(new StubTrigger(), "text", states: null, options, token);

    /// <summary>
    /// The default. Nothing about the loop changes when no per-rule limit is set: a run nobody cancels
    /// dispatches every rule.
    /// </summary>
    [Fact]
    public async Task Runs_every_rule_when_no_per_rule_limit_is_set()
    {
        var (engine, sentToAgents) = Build();

        var result = await Run(engine, new RuleTriggerOptions { SendMessageDelayMs = 0 });

        Assert.Equal(["agent-1", "agent-2"], sentToAgents);
        Assert.Equal(["conv-agent-1", "conv-agent-2"], result);
    }

    /// <summary>
    /// The point of the per-rule limit: the rule that outstays it is the only one given up on, and the
    /// rule behind it still gets its turn. Before the limit existed there was no way to express this -
    /// the only cancellation the loop understood ended the run.
    /// </summary>
    [Fact]
    public async Task A_rule_that_outstays_its_limit_does_not_cost_the_next_rule_its_turn()
    {
        var (engine, sentToAgents) = Build(
            sendDuration: new Dictionary<string, TimeSpan> { ["agent-1"] = TimeSpan.FromMilliseconds(600) });

        var result = await Run(engine, new RuleTriggerOptions
        {
            SendMessageDelayMs = 1,
            RuleTimeout = TimeSpan.FromMilliseconds(100)
        });

        // Both were dispatched: the first outstayed its limit, the second still ran.
        Assert.Equal(["agent-1", "agent-2"], sentToAgents);

        // Only the second is reported. The first conversation exists - its turn finished - but the rule
        // was cancelled before it could hand the id back, which is what OnConversationCreated is for.
        Assert.Equal(["conv-agent-2"], result);
    }

    /// <summary>
    /// The other half of the split: a cancelled run still stops dispatching, and still hands back what it
    /// started, even though a per-rule limit is now in play.
    /// </summary>
    [Fact]
    public async Task A_cancelled_run_still_stops_the_rules_behind_it()
    {
        using var cts = new CancellationTokenSource();
        var (engine, sentToAgents) = Build(onSendStarted: agentId =>
        {
            if (agentId == "agent-1")
            {
                cts.Cancel();
            }
        });

        var ex = await Assert.ThrowsAsync<RuleTriggerCanceledException>(() => Run(engine, new RuleTriggerOptions
        {
            SendMessageDelayMs = 1,
            RuleTimeout = TimeSpan.FromMinutes(5)
        }, cts.Token));

        // The second rule was never dispatched, and the run reports itself cancelled rather than quietly
        // treating the caller's token as one rule's limit.
        Assert.Equal(["agent-1"], sentToAgents);
        Assert.NotNull(ex.ConversationIds);
    }
}
