using System;
using System.Collections.Generic;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using BotSharp.Plugin.AgentTesting.Runtime;
using BotSharp.Plugin.AgentTesting.Models;
using Xunit;

namespace BotSharp.Core.UnitTests.AgentTesting;

/// <summary>
/// provider 只做一件事：判断"当前这个会话是不是正在跑测试用例"，是则接管，否则完全放过。
///
/// 它按 conversationId 查注册表，而不是用 AsyncLocal。AsyncLocal 依赖 ExecutionContext 流动，
/// 一旦某条执行路径经过后台队列或 SideCar 就会静默丢失——丢失的后果不是测试失败，而是未 mock
/// 的工具被真实执行（真发电话、真建工单）。这几个测试钉住的就是"非测试会话一律不接管"和
/// "测试会话一律接管"这两条边界。
/// </summary>
public class TestMockExecutorProviderTests
{
    private const string TestConversation = "3f1c9d2e-0000-4a11-9b21-aaaaaaaaaaaa";
    private const string NormalConversation = "9a2b8c7d-1111-4c22-8f33-bbbbbbbbbbbb";

    private static TestMockExecutorProvider Build(IAgentTestRunRegistry registry, string conversationId)
    {
        var conversations = new Mock<IConversationService>();
        conversations.SetupGet(c => c.ConversationId).Returns(conversationId);
        return new TestMockExecutorProvider(
            registry,
            conversations.Object,
            new Mock<IConversationStateService>().Object,
            NullLogger<TestMockExecutorProvider>.Instance);
    }

    private static ActiveTestRun RunFor(string conversationId, params TestToolMock[] mocks) => new()
    {
        ConversationId = conversationId,
        CaseId = "case-1",
        Mocks = mocks,
        UnmockedToolPolicy = UnmockedToolPolicies.Block,
        AllowedFunctions = new HashSet<string>(ControlFlowFunctions.Default, StringComparer.OrdinalIgnoreCase),
        ForceBlockedFunctions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    };

    [Fact]
    public void Does_not_take_over_a_conversation_that_is_not_under_test()
    {
        var registry = new AgentTestRunRegistry();
        registry.Register(RunFor(TestConversation));

        var provider = Build(registry, NormalConversation);

        Assert.Null(provider.TryResolve("create_work_order", new Agent()));
    }

    [Fact]
    public void Takes_over_every_function_inside_a_conversation_under_test()
    {
        var registry = new AgentTestRunRegistry();
        registry.Register(RunFor(TestConversation, new TestToolMock { FunctionName = "create_work_order" }));

        var provider = Build(registry, TestConversation);

        Assert.NotNull(provider.TryResolve("create_work_order", new Agent()));
    }

    [Fact]
    public void Takes_over_an_unmocked_function_too_so_that_it_can_be_blocked()
    {
        // 关键：未 mock 的函数也必须被接管，否则会落到内置链上真实执行。
        var registry = new AgentTestRunRegistry();
        registry.Register(RunFor(TestConversation));

        var provider = Build(registry, TestConversation);

        Assert.NotNull(provider.TryResolve("send_text_message", new Agent()));
    }

    [Fact]
    public void Leaves_control_flow_functions_to_the_real_implementation()
    {
        // 阻断 route_to_agent 等于让 agent 走不动，用例永远跑不到断言。
        var registry = new AgentTestRunRegistry();
        registry.Register(RunFor(TestConversation));

        var provider = Build(registry, TestConversation);

        Assert.Null(provider.TryResolve("route_to_agent", new Agent()));
        Assert.Null(provider.TryResolve("util-routing-fallback_to_router", new Agent()));
    }

    [Fact]
    public void Takes_over_a_util_prefixed_function_that_is_not_control_flow()
    {
        // 允许列表必须是精确的五个名字，不能退化成"util- 前缀一律放行"：
        // util-twilio-outbound_phone_call 是真实打电话的函数，若判定改成前缀匹配，
        // 这里会被误放行，测试跑一遍就真的拨出电话。这条断言在前缀规则下会失败
        // （前缀匹配会让它命中 null，而正确实现必须接管/NotNull）。
        var registry = new AgentTestRunRegistry();
        registry.Register(RunFor(TestConversation));

        var provider = Build(registry, TestConversation);

        Assert.NotNull(provider.TryResolve("util-twilio-outbound_phone_call", new Agent()));
    }

    [Fact]
    public void A_force_blocked_function_is_taken_over_even_if_it_is_on_the_allow_list()
    {
        var run = RunFor(TestConversation);
        run.ForceBlockedFunctions.Add("response_to_user");
        var registry = new AgentTestRunRegistry();
        registry.Register(run);

        var provider = Build(registry, TestConversation);

        Assert.NotNull(provider.TryResolve("response_to_user", new Agent()));
    }

    [Fact]
    public void Unregister_returns_the_conversation_to_normal_behaviour()
    {
        var registry = new AgentTestRunRegistry();
        registry.Register(RunFor(TestConversation));
        registry.Unregister(TestConversation);

        var provider = Build(registry, TestConversation);

        Assert.Null(provider.TryResolve("create_work_order", new Agent()));
    }

    [Fact]
    public void A_null_conversation_id_never_matches()
    {
        var registry = new AgentTestRunRegistry();
        registry.Register(RunFor(TestConversation));

        var provider = Build(registry, null!);

        Assert.Null(provider.TryResolve("create_work_order", new Agent()));
    }
}
