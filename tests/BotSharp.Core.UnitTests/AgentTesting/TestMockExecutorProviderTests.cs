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
/// The provider does exactly one thing: decide whether the current conversation is running a test
/// case, take over if it is, and pass through completely if it is not.
///
/// It looks the conversation up in the registry by conversationId rather than using AsyncLocal.
/// AsyncLocal depends on ExecutionContext flowing, and is silently lost the moment an execution path
/// crosses a background queue or SideCar -- and losing it does not produce a failing test, it
/// produces unmocked tools executing for real: a real phone call, a real work order. These tests pin
/// both edges: never take over a non-test conversation, always take over a test one.
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
        // The crucial one: an unmocked function must be taken over too, or it falls through to the
        // built-in chain and executes for real.
        var registry = new AgentTestRunRegistry();
        registry.Register(RunFor(TestConversation));

        var provider = Build(registry, TestConversation);

        Assert.NotNull(provider.TryResolve("send_text_message", new Agent()));
    }

    [Fact]
    public void Leaves_control_flow_functions_to_the_real_implementation()
    {
        // Blocking route_to_agent means the agent cannot move, and the case never reaches its
        // assertions at all.
        var registry = new AgentTestRunRegistry();
        registry.Register(RunFor(TestConversation));

        var provider = Build(registry, TestConversation);

        Assert.Null(provider.TryResolve("route_to_agent", new Agent()));
        Assert.Null(provider.TryResolve("util-routing-fallback_to_router", new Agent()));
    }

    [Fact]
    public void Takes_over_a_util_prefixed_function_that_is_not_control_flow()
    {
        // The allow list must be those exact five names and must never degrade into "anything
        // prefixed util- passes". util-twilio-outbound_phone_call really places a phone call, so a
        // prefix rule would wave it through and one test run would really dial out. This assertion
        // fails under a prefix rule -- prefix matching would return null here, whereas the correct
        // implementation has to take over.
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
