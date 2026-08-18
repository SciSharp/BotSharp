using System.Threading.Tasks;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using BotSharp.Plugin.AgentTesting.Runtime;
using BotSharp.Plugin.AgentTesting.Models;
using Xunit;

namespace BotSharp.Core.UnitTests.AgentTesting;

/// <summary>
/// Three things about mock execution semantics have to be right:
/// 1) on a mock hit, the fake return is written to message.Content, which is what the LLM reads next
///    turn;
/// 2) when unmocked under the Block policy, the real implementation must never be reached, and the
///    turn has to fail explicitly rather than continue silently;
/// 3) on a mock hit it must be able to write conversation state -- for plenty of real functions the
///    actual "output" IS the state write, and returning only a value leaves later functions unable to
///    read what they need and the whole case collapses.
/// </summary>
public class MockFunctionExecutorTests
{
    private static (MockFunctionExecutor executor, Mock<IConversationStateService> state) Build(
        ActiveTestRun run, string functionName)
    {
        var state = new Mock<IConversationStateService>();
        var executor = new MockFunctionExecutor(run, functionName, state.Object, NullLogger.Instance);
        return (executor, state);
    }

    private static ActiveTestRun Run(string policy, params TestToolMock[] mocks) => new()
    {
        ConversationId = "conv-1",
        CaseId = "case-1",
        Mocks = mocks,
        UnmockedToolPolicy = policy
    };

    private static RoleDialogModel Call(string function, string? args = null) =>
        new(AgentRole.Assistant, string.Empty) { FunctionName = function, FunctionArgs = args };

    [Fact]
    public async Task Writes_the_canned_result_into_the_message()
    {
        var run = Run(UnmockedToolPolicies.Block,
            new TestToolMock { FunctionName = "get_work_order", ResultContent = """{"status":"Open"}""" });
        var (executor, _) = Build(run, "get_work_order");
        var message = Call("get_work_order");

        var ok = await executor.ExecuteAsync(message);

        Assert.True(ok);
        Assert.Equal("""{"status":"Open"}""", message.Content);
    }

    [Fact]
    public async Task Applies_stop_completion_when_the_mock_says_so()
    {
        var run = Run(UnmockedToolPolicies.Block,
            new TestToolMock { FunctionName = "ask_user", ResultContent = "ok", StopCompletion = true });
        var (executor, _) = Build(run, "ask_user");
        var message = Call("ask_user");

        await executor.ExecuteAsync(message);

        Assert.True(message.StopCompletion);
    }

    [Fact]
    public async Task Applies_the_mocks_state_writes()
    {
        var run = Run(UnmockedToolPolicies.Block, new TestToolMock
        {
            FunctionName = "get_work_order",
            ResultContent = "ok",
            StateWrites = [new TestState { Key = "wo_id", Value = "123", ActiveRounds = 5 }]
        });
        var (executor, state) = Build(run, "get_work_order");

        await executor.ExecuteAsync(Call("get_work_order"));

        state.Verify(s => s.SetState(
            "wo_id", "123", It.IsAny<bool>(), 5, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()),
            Times.Once);
    }

    [Fact]
    public async Task Blocks_an_unmocked_tool_under_the_block_policy()
    {
        var run = Run(UnmockedToolPolicies.Block);
        var (executor, _) = Build(run, "send_text_message");
        var message = Call("send_text_message");

        var ok = await executor.ExecuteAsync(message);

        Assert.False(ok);
        Assert.Equal("[agent-test] blocked unmocked tool: send_text_message", message.Content);
        Assert.True(message.StopCompletion);
    }

    [Fact]
    public async Task Records_every_call_it_handled_with_the_right_outcome()
    {
        var run = Run(UnmockedToolPolicies.Block,
            new TestToolMock { FunctionName = "get_work_order", ResultContent = "ok" });
        run.CurrentTurnIndex = 2;

        await Build(run, "get_work_order").executor.ExecuteAsync(Call("get_work_order", """{"woNum":"B1"}"""));
        await Build(run, "send_text_message").executor.ExecuteAsync(Call("send_text_message"));

        var calls = run.ObservedCalls;
        Assert.Equal(2, calls.Count);
        Assert.Equal("Mocked", calls[0].Outcome);
        Assert.Equal("""{"woNum":"B1"}""", calls[0].ArgsJson);
        Assert.Equal(2, calls[0].TurnIndex);
        Assert.Equal("Blocked", calls[1].Outcome);
    }

    [Fact]
    public async Task Records_the_canary_so_the_runner_can_prove_the_seam_is_live()
    {
        var run = Run(UnmockedToolPolicies.Block);
        var (executor, _) = Build(run, AgentTestCanary.FunctionName);

        await executor.ExecuteAsync(Call(AgentTestCanary.FunctionName));

        Assert.True(run.CanaryIntercepted);
    }

    [Fact]
    public async Task Advances_the_call_ordinal_across_successive_calls_to_the_same_function()
    {
        // Unit-testing Match(..., callOrdinal) over in ToolMockMatcherTests is not enough. This
        // proves the executor itself really wires _run.NextCallOrdinal(_functionName) into Match's
        // ordinal parameter and that nobody quietly replaced it with a hardcoded 0 -- a hardcoded 0
        // would make the second call below resolve to the first mock too, and the assertion would
        // catch it.
        var run = Run(UnmockedToolPolicies.Block,
            new TestToolMock { FunctionName = "get_work_order", CallIndex = 0, ResultContent = "first-call" },
            new TestToolMock { FunctionName = "get_work_order", CallIndex = 1, ResultContent = "second-call" });

        var first = Call("get_work_order");
        await Build(run, "get_work_order").executor.ExecuteAsync(first);

        var second = Call("get_work_order");
        await Build(run, "get_work_order").executor.ExecuteAsync(second);

        Assert.Equal("first-call", first.Content);
        Assert.Equal("second-call", second.Content);
    }
}
