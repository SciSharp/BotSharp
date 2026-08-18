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
/// mock 的执行语义有三件事必须对：
/// 1) 命中 mock 时把假返回写进 message.Content（LLM 下一轮就读这个）；
/// 2) 未 mock 且策略为 Block 时，真实实现一次都不能被调到，且要让本轮明确失败而不是静默继续；
/// 3) 命中 mock 时要能写会话 state——大量真实函数的"输出"其实是 state 写入，
///    只给返回值会让后续函数读不到数据而全线崩（见 IFunctionCallback-full-detail-report.md）。
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
        // 只在 ToolMockMatcherTests 里单测 Match(..., callOrdinal) 不够：这里要证明
        // executor 自己真的把 _run.NextCallOrdinal(_functionName) 接到了 Match 的调用序号参数上，
        // 不是被谁悄悄改成硬编码 0——硬编码 0 会让下面第二次调用也命中第一个 mock，断言就会炸。
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
