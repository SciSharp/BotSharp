using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Agents.Settings;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Routing.Settings;
using BotSharp.Abstraction.Templating;
using BotSharp.Core.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BotSharp.Core.UnitTests.Routing;

/// <summary>
/// A model reply can ask for several tools at once, and every provider here reports the whole set
/// on <see cref="RoleDialogModel.ToolCalls"/>. The routing engine read only the singular
/// FunctionName beside it, so calls two and three were dropped with no error, no log and nothing
/// told to the model -- it simply asked for them again on the next turn, or answered without them.
///
/// These tests pin the fixed behaviour: every call runs, results are appended in order, and the
/// model is asked again once for the whole batch rather than once per call.
/// </summary>
public class InvokeAgentToolCallTests
{
    private const string AgentId = "agent-1";

    /// <summary>
    /// Returns the replies it was given, one per round, and a plain answer once they run out --
    /// which is what stops the recursion at the end of a test.
    /// </summary>
    private sealed class ScriptedChatCompletion(params RoleDialogModel[] replies) : IChatCompletion
    {
        private readonly Queue<RoleDialogModel> _replies = new(replies);

        public string Provider => "test-provider";
        public string Model => "test-model";

        /// <summary>How many times the model was asked. One per LLM round trip.</summary>
        public int Rounds { get; private set; }

        public void SetModelName(string model) { }

        public Task<RoleDialogModel> GetChatCompletions(Agent agent, List<RoleDialogModel> conversations)
        {
            Rounds++;
            var reply = _replies.Count > 0
                ? _replies.Dequeue()
                : new RoleDialogModel(AgentRole.Assistant, "final answer");
            return Task.FromResult(reply);
        }
    }

    private sealed record ExecutedCall(string Name, string? ToolCallId, string? Args);

    /// <summary>
    /// Everything InvokeAgent reaches for, wired to stubs, plus the two things a test looks at:
    /// the calls that actually reached the executor and how many LLM rounds it took.
    /// </summary>
    private sealed class Harness
    {
        public required RoutingService Routing { get; init; }
        public required ScriptedChatCompletion Chat { get; init; }
        public required List<ExecutedCall> Executed { get; init; }
        public required Mock<IRoutingContext> Context { get; init; }
        public required Mock<IResponseTemplateService> Template { get; init; }
    }

    private static Harness BuildHarness(
        RoleDialogModel[] replies,
        Action<RoleDialogModel>? onFunctionInvoked = null)
    {
        var agent = new Agent
        {
            Id = AgentId,
            Name = "tester",
            LlmConfig = new AgentLlmConfig
            {
                Provider = "test-provider",
                Model = "test-model",
                MaxRecursionDepth = 10
            }
        };

        var agentService = new Mock<IAgentService>();
        agentService.Setup(x => x.LoadAgent(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(agent);

        var context = new Mock<IRoutingContext>();
        context.Setup(x => x.GetCurrentAgentId()).Returns(AgentId);

        var executed = new List<ExecutedCall>();
        var routingStub = new Mock<IRoutingService>();
        routingStub.SetupGet(x => x.Context).Returns(context.Object);
        routingStub
            .Setup(x => x.InvokeFunction(It.IsAny<string>(), It.IsAny<RoleDialogModel>(), It.IsAny<InvokeFunctionOptions>()))
            .Returns((string name, RoleDialogModel message, InvokeFunctionOptions? _) =>
            {
                executed.Add(new ExecutedCall(name, message.ToolCallId, message.FunctionArgs));
                message.Content = $"{name} result";
                onFunctionInvoked?.Invoke(message);
                return Task.FromResult(true);
            });

        // No template by default: the branch that renders one ends the turn, and only the test
        // that is about that branch wants it.
        var template = new Mock<IResponseTemplateService>();
        template
            .Setup(x => x.RenderFunctionResponse(It.IsAny<string>(), It.IsAny<RoleDialogModel>()))
            .ReturnsAsync(string.Empty);

        // Not in conversation mode, so Persist is a no-op and no storage is needed.
        var conversation = new Mock<IConversationService>();
        conversation.Setup(x => x.IsConversationMode()).Returns(false);

        var chat = new ScriptedChatCompletion(replies);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(agentService.Object);
        services.AddSingleton(new AgentSettings());
        services.AddSingleton(new Mock<IConversationStateService>().Object);
        services.AddSingleton<IChatCompletion>(chat);
        services.AddSingleton(routingStub.Object);
        services.AddSingleton(template.Object);
        services.AddSingleton(conversation.Object);

        var routing = new RoutingService(
            services.BuildServiceProvider(),
            new RoutingSettings(),
            context.Object,
            NullLogger<RoutingService>.Instance);

        return new Harness
        {
            Routing = routing,
            Chat = chat,
            Executed = executed,
            Context = context,
            Template = template
        };
    }

    /// <summary>A reply asking for the given calls, reported the way every provider reports them.</summary>
    private static RoleDialogModel ToolReply(params (string Name, string Args, string Id)[] calls)
    {
        var toolCalls = calls.Select(x => new LlmToolCall(x.Id, x.Name, x.Args)).ToList();
        var first = toolCalls.First();

        return new RoleDialogModel(AgentRole.Function, string.Empty)
        {
            CurrentAgentId = AgentId,
            ToolCallId = first.Id,
            FunctionName = first.FunctionName,
            FunctionArgs = first.FunctionArgs,
            ToolCalls = toolCalls
        };
    }

    private static List<RoleDialogModel> NewDialogs()
        => [new RoleDialogModel(AgentRole.User, "what is the weather and the time?") { MessageId = "msg-1" }];

    [Fact]
    public async Task InvokeAgent_RunsEveryCallOfTheReply()
    {
        var harness = BuildHarness(
        [
            ToolReply(
                ("get_weather", "{\"city\":\"Chicago\"}", "call_1"),
                ("get_time", "{\"zone\":\"CST\"}", "call_2"),
                ("get_rate", "{\"pair\":\"USDCNY\"}", "call_3"))
        ]);

        var dialogs = NewDialogs();
        await harness.Routing.InvokeAgent(AgentId, dialogs);

        Assert.Equal(
            ["get_weather", "get_time", "get_rate"],
            harness.Executed.Select(x => x.Name));

        // Each result has to go back under the id of the call it answers, or the provider cannot
        // match them up.
        Assert.Equal(["call_1", "call_2", "call_3"], harness.Executed.Select(x => x.ToolCallId));
        Assert.Equal(
            ["{\"city\":\"Chicago\"}", "{\"zone\":\"CST\"}", "{\"pair\":\"USDCNY\"}"],
            harness.Executed.Select(x => x.Args));
    }

    [Fact]
    public async Task InvokeAgent_AppendsEveryResultThenAsksTheModelOnce()
    {
        var harness = BuildHarness(
        [
            ToolReply(
                ("get_weather", "{}", "call_1"),
                ("get_time", "{}", "call_2"))
        ]);

        var dialogs = NewDialogs();
        await harness.Routing.InvokeAgent(AgentId, dialogs);

        var functionResults = dialogs.Where(x => x.Role == AgentRole.Function).ToList();
        Assert.Equal(["get_weather result", "get_time result"], functionResults.Select(x => x.Content));
        Assert.Equal(["call_1", "call_2"], functionResults.Select(x => x.ToolCallId));

        Assert.Equal(AgentRole.Assistant, dialogs.Last().Role);
        Assert.Equal("final answer", dialogs.Last().Content);

        // Two rounds, not three: the batch is answered once, not once per call.
        Assert.Equal(2, harness.Chat.Rounds);
    }

    /// <summary>
    /// Recursion depth is a budget on how many times the model gets to speak. Running a batch
    /// inside one turn must not spend it per call, or a reply asking for four tools would exhaust
    /// the default depth of three before the model ever saw a result.
    /// </summary>
    [Fact]
    public async Task InvokeAgent_SpendsOneRecursionPerRoundNotPerCall()
    {
        var harness = BuildHarness(
        [
            ToolReply(
                ("a", "{}", "call_1"),
                ("b", "{}", "call_2"),
                ("c", "{}", "call_3"))
        ]);

        await harness.Routing.InvokeAgent(AgentId, NewDialogs());

        harness.Context.Verify(x => x.IncreaseRecursiveCounter(), Times.Exactly(2));
    }

    /// <summary>
    /// A provider that fills only the singular fields -- every provider outside OpenAI and
    /// Anthropic today -- has to keep working untouched.
    /// </summary>
    [Fact]
    public async Task InvokeAgent_ReadsTheSingularFieldsWhenToolCallsIsAbsent()
    {
        var reply = new RoleDialogModel(AgentRole.Function, string.Empty)
        {
            CurrentAgentId = AgentId,
            ToolCallId = "call_1",
            FunctionName = "get_weather",
            FunctionArgs = "{\"city\":\"Chicago\"}"
        };

        var harness = BuildHarness([reply]);
        await harness.Routing.InvokeAgent(AgentId, NewDialogs());

        var call = Assert.Single(harness.Executed);
        Assert.Equal("get_weather", call.Name);
        Assert.Equal("call_1", call.ToolCallId);
    }

    /// <summary>
    /// Names arrive on <see cref="RoleDialogModel.ToolCalls"/> exactly as the model produced them,
    /// so the repair that the singular field has always had has to be applied to each of them.
    /// </summary>
    [Fact]
    public async Task InvokeAgent_NormalizesEveryCallsName()
    {
        var harness = BuildHarness(
        [
            ToolReply(
                ("weather_agent.get_weather", "{}", "call_1"),
                ("clock/get_time", "{}", "call_2"))
        ]);

        await harness.Routing.InvokeAgent(AgentId, NewDialogs());

        Assert.Equal(["get_weather", "get_time"], harness.Executed.Select(x => x.Name));
    }

    /// <summary>
    /// A function that answers the user itself ends the turn. The calls behind it were asked for
    /// without knowing that, and running them into a finished turn would produce results nothing
    /// reads -- so the batch stops there, and no further round is asked of the model.
    /// </summary>
    [Fact]
    public async Task InvokeAgent_StopsAtTheCallThatEndsTheTurn()
    {
        var harness = BuildHarness(
            [
                ToolReply(
                    ("get_weather", "{}", "call_1"),
                    ("hand_off_to_human", "{}", "call_2"),
                    ("get_rate", "{}", "call_3"))
            ],
            onFunctionInvoked: message =>
            {
                if (message.FunctionName == "hand_off_to_human")
                {
                    message.StopCompletion = true;
                    message.Content = "A colleague will take it from here.";
                }
            });

        var dialogs = NewDialogs();
        await harness.Routing.InvokeAgent(AgentId, dialogs);

        Assert.Equal(["get_weather", "hand_off_to_human"], harness.Executed.Select(x => x.Name));
        Assert.Equal(1, harness.Chat.Rounds);

        Assert.Equal(AgentRole.Assistant, dialogs.Last().Role);
        Assert.Equal("A colleague will take it from here.", dialogs.Last().Content);
    }

    /// <summary>
    /// A rendered response template answers in the function's place and has always ended the turn
    /// too. It stops the batch for the same reason <see cref="RoleDialogModel.StopCompletion"/>
    /// does.
    /// </summary>
    [Fact]
    public async Task InvokeAgent_StopsWhenAResponseTemplateAnswers()
    {
        var harness = BuildHarness(
        [
            ToolReply(
                ("get_weather", "{}", "call_1"),
                ("get_time", "{}", "call_2"))
        ]);

        harness.Template
            .Setup(x => x.RenderFunctionResponse(It.IsAny<string>(), It.IsAny<RoleDialogModel>()))
            .ReturnsAsync("It is sunny in Chicago.");

        var dialogs = NewDialogs();
        await harness.Routing.InvokeAgent(AgentId, dialogs);

        Assert.Equal(["get_weather"], harness.Executed.Select(x => x.Name));
        Assert.Equal(1, harness.Chat.Rounds);
        Assert.Equal("It is sunny in Chicago.", dialogs.Last().Content);
    }

    /// <summary>
    /// A reply with no calls at all still ends up as a plain assistant message.
    /// </summary>
    [Fact]
    public async Task InvokeAgent_KeepsThePlainAnswerPathUnchanged()
    {
        var harness = BuildHarness(
        [
            new RoleDialogModel(AgentRole.Assistant, "It is sunny.") { CurrentAgentId = AgentId }
        ]);

        var dialogs = NewDialogs();
        await harness.Routing.InvokeAgent(AgentId, dialogs);

        Assert.Empty(harness.Executed);
        Assert.Equal(1, harness.Chat.Rounds);
        Assert.Equal(AgentRole.Assistant, dialogs.Last().Role);
        Assert.Equal("It is sunny.", dialogs.Last().Content);
    }
}
