using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Conversations.Settings;
using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Users;
using BotSharp.Abstraction.Utilities;
using BotSharp.Logger.Hooks;
using BotSharp.Plugin.AgentTesting.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BotSharp.Core.UnitTests.AgentTesting;

/// <summary>
/// BotSharp's rate limiting counts human behaviour, and a regression suite does not look like one: it
/// opens a conversation per case per model and drives the turns as fast as the model answers.
///
/// Two of its three guards therefore have to stand aside for a harness conversation, and BOTH of them
/// matter -- the conversation quota is the one that surfaced first, but the two-second gap between
/// messages blocks any case with authored history outright, because injected history lands with the
/// current timestamp and the first real turn follows it immediately.
///
/// The direction of these tests matters as much as the assertions: a probe that wrongly answers yes
/// exempts real user traffic from the limits it is meant to be held to, so every "real traffic" case
/// below is checked for still being limited.
/// </summary>
public class SyntheticConversationExemptionTests
{
    private const string ConversationId = "conv-under-test";

    private static RateLimitConversationHook BuildHook(
        IServiceProvider services, List<RoleDialogModel> dialogs)
    {
        var hook = new RateLimitConversationHook(
            services, NullLogger<RateLimitConversationHook>.Instance);

        // Dialogs is only reachable through the base class's own loader, which is also how the real
        // conversation path populates it.
        hook.OnDialogsLoaded(dialogs).GetAwaiter().GetResult();
        return hook;
    }

    /// <summary>
    /// Everything OnMessageReceived reaches for. <paramref name="probe"/> null registers none at all,
    /// which is the normal deployment and must exempt nobody.
    /// </summary>
    private static IServiceProvider BuildServices(
        ISyntheticConversationProbe? probe,
        int conversationsToday,
        string conversationId = ConversationId)
    {
        var services = new ServiceCollection();

        services.AddSingleton(new ConversationSetting
        {
            RateLimit = new RateLimitSetting
            {
                MaxConversationPerDay = 100,
                MaxInputLengthPerRequest = 1024,
                MinTimeSecondsBetweenMessages = 2
            }
        });

        var states = new Mock<IConversationStateService>();
        // Blank, exactly as a harness conversation leaves it: the guards are skipped for Phone, Email
        // and Database channels, and the harness deliberately seeds no channel because production
        // routing branches on that key.
        states.Setup(x => x.GetState(It.IsAny<string>(), It.IsAny<string>())).Returns(string.Empty);
        services.AddSingleton(states.Object);

        var identity = new Mock<IUserIdentity>();
        // Empty, as it is in a BackgroundService with no HTTP context. Note the Mongo filter drops an
        // empty UserId rather than matching on it, so this counts every conversation in the instance.
        identity.Setup(x => x.Id).Returns(string.Empty);
        services.AddSingleton(identity.Object);

        var conversations = new Mock<IConversationService>();
        conversations.Setup(x => x.ConversationId).Returns(conversationId);
        conversations
            .Setup(x => x.GetConversations(It.IsAny<ConversationFilter>()))
            .ReturnsAsync(new PagedItems<Conversation>
            {
                Count = conversationsToday,
                Items = []
            });
        services.AddSingleton(conversations.Object);

        if (probe != null)
        {
            services.AddSingleton(probe);
        }

        return services.BuildServiceProvider();
    }

    private static List<RoleDialogModel> TwoUserMessagesOneSecondApart() =>
    [
        new RoleDialogModel(AgentRole.User, "earlier") { CreatedAt = DateTime.UtcNow.AddSeconds(-1) },
        new RoleDialogModel(AgentRole.User, "now") { CreatedAt = DateTime.UtcNow }
    ];

    // ------------------------------------------------------------------ the probe

    [Fact]
    public void The_probe_recognises_a_conversation_the_harness_registered()
    {
        var registry = new AgentTestRunRegistry();
        registry.Register(new ActiveTestRun { ConversationId = ConversationId, CaseId = "case-1" });

        var probe = new AgentTestSyntheticConversationProbe(registry);

        Assert.True(probe.IsSynthetic(ConversationId));
    }

    [Theory]
    [InlineData("some-other-conversation")]
    [InlineData("")]
    [InlineData(null)]
    public void The_probe_answers_no_for_anything_it_does_not_recognise(string? conversationId)
    {
        // Answering yes here would exempt real user traffic from the limits it exists to be held to,
        // which is a far worse failure than a test being rate limited.
        var registry = new AgentTestRunRegistry();
        registry.Register(new ActiveTestRun { ConversationId = ConversationId, CaseId = "case-1" });

        var probe = new AgentTestSyntheticConversationProbe(registry);

        Assert.False(probe.IsSynthetic(conversationId!));
    }

    [Fact]
    public void The_probe_stops_recognising_a_conversation_once_the_case_finishes()
    {
        // The runner unregisters in a finally block. If the probe kept saying yes afterwards, that
        // conversation id would stay exempt from rate limiting for the life of the process.
        var registry = new AgentTestRunRegistry();
        registry.Register(new ActiveTestRun { ConversationId = ConversationId, CaseId = "case-1" });
        var probe = new AgentTestSyntheticConversationProbe(registry);

        registry.Unregister(ConversationId);

        Assert.False(probe.IsSynthetic(ConversationId));
    }

    // ------------------------------------------------------------------ the quota guard

    [Fact]
    public async Task Real_traffic_over_the_quota_is_still_stopped()
    {
        // The guard has to keep working. With no probe registered -- the normal deployment -- nothing
        // is exempt.
        var services = BuildServices(probe: null, conversationsToday: 107);
        var hook = BuildHook(services, []);
        var message = new RoleDialogModel(AgentRole.User, "hello");

        await hook.OnMessageReceived(message);

        Assert.True(message.StopCompletion);
        Assert.Contains("exceeds the system maximum of 100", message.Content);
    }

    [Fact]
    public async Task A_harness_conversation_over_the_quota_runs_anyway()
    {
        // The reported failure. 107 conversations across the whole instance in 24 hours, of which the
        // harness opened 30 -- and because an empty UserId drops the filter rather than matching on
        // it, the harness was measured against all 107 and every case failed.
        var registry = new AgentTestRunRegistry();
        registry.Register(new ActiveTestRun { ConversationId = ConversationId, CaseId = "case-1" });

        var services = BuildServices(
            new AgentTestSyntheticConversationProbe(registry), conversationsToday: 107);
        var hook = BuildHook(services, []);
        var message = new RoleDialogModel(AgentRole.User, "hello");

        await hook.OnMessageReceived(message);

        Assert.False(message.StopCompletion);
        Assert.Equal("hello", message.Content);
    }

    [Fact]
    public async Task A_conversation_the_probe_does_not_claim_is_still_limited()
    {
        // A harness being active must not exempt everything else running at the same time.
        var registry = new AgentTestRunRegistry();
        registry.Register(new ActiveTestRun { ConversationId = "a-different-case", CaseId = "case-1" });

        var services = BuildServices(
            new AgentTestSyntheticConversationProbe(registry), conversationsToday: 107);
        var hook = BuildHook(services, []);
        var message = new RoleDialogModel(AgentRole.User, "hello");

        await hook.OnMessageReceived(message);

        Assert.True(message.StopCompletion);
    }

    // ------------------------------------------------------------------ the frequency guard

    [Fact]
    public async Task Real_traffic_sending_faster_than_the_minimum_gap_is_still_stopped()
    {
        var services = BuildServices(probe: null, conversationsToday: 1);
        var hook = BuildHook(services, TwoUserMessagesOneSecondApart());
        var message = new RoleDialogModel(AgentRole.User, "hello");

        await hook.OnMessageReceived(message);

        Assert.True(message.StopCompletion);
        Assert.Contains("frequency", message.Content);
    }

    [Fact]
    public async Task A_harness_conversation_is_not_held_to_the_minimum_gap()
    {
        // This is the guard that blocks any case with authored history: injected history lands with
        // the current timestamp, so the first real turn follows the last history message by about zero
        // seconds and trips a two-second minimum every time. It would also make multi-turn cases
        // flaky whenever the model happens to answer quickly.
        var registry = new AgentTestRunRegistry();
        registry.Register(new ActiveTestRun { ConversationId = ConversationId, CaseId = "case-1" });

        var services = BuildServices(
            new AgentTestSyntheticConversationProbe(registry), conversationsToday: 1);
        var hook = BuildHook(services, TwoUserMessagesOneSecondApart());
        var message = new RoleDialogModel(AgentRole.User, "hello");

        await hook.OnMessageReceived(message);

        Assert.False(message.StopCompletion);
    }

    // ------------------------------------------------------------------ the input length guard

    [Fact]
    public async Task An_over_long_message_is_still_rejected_in_a_harness_conversation()
    {
        // Kept in force on purpose. That guard is about one message being too large for the model,
        // which is a real condition a test should surface rather than be excused from -- unlike the
        // two volume guards, which measure how much a human has been using the system.
        var registry = new AgentTestRunRegistry();
        registry.Register(new ActiveTestRun { ConversationId = ConversationId, CaseId = "case-1" });

        var services = BuildServices(
            new AgentTestSyntheticConversationProbe(registry), conversationsToday: 1);
        var hook = BuildHook(services, []);
        var message = new RoleDialogModel(AgentRole.User, new string('x', 2000));

        await hook.OnMessageReceived(message);

        Assert.True(message.StopCompletion);
        Assert.Contains("characters", message.Content);
    }

    // ------------------------------------------------------------------ a misbehaving probe

    [Fact]
    public async Task A_probe_that_throws_fails_closed()
    {
        // Failing open would let a bug in a probe silently lift the limits for real traffic. The worst
        // case here is a harness message being rate limited, which is visible and recoverable.
        var probe = new Mock<ISyntheticConversationProbe>();
        probe.Setup(x => x.IsSynthetic(It.IsAny<string>())).Throws(new InvalidOperationException("boom"));

        var services = BuildServices(probe.Object, conversationsToday: 107);
        var hook = BuildHook(services, []);
        var message = new RoleDialogModel(AgentRole.User, "hello");

        await hook.OnMessageReceived(message);

        Assert.True(message.StopCompletion);
    }
}
