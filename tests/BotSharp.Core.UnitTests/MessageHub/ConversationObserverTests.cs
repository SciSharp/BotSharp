using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Enums;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.MessageHub.Models;
using BotSharp.Abstraction.Routing;
using BotSharp.Core.MessageHub.Observers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BotSharp.Core.UnitTests.MessageHub;

public class ConversationObserverTests
{
    [Theory]
    [InlineData(ChatEvent.OnIndicationReceived)]
    [InlineData(ChatEvent.OnReceiveLlmStreamMessage)]
    [InlineData(ChatEvent.OnMessageReceivedFromAssistant)]
    public void OnNext_ReachesTheListenerRegisteredForTheEvent(string eventName)
    {
        var observed = new List<string>();
        var observer = BuildObserver();
        observer.SetEventListeners(new Dictionary<string, Func<HubObserveData<RoleDialogModel>, Task>>
        {
            [eventName] = data =>
            {
                observed.Add(data.Data.Content);
                return Task.CompletedTask;
            }
        });

        observer.OnNext(BuildEvent(eventName, "chunk"));

        Assert.Equal(new[] { "chunk" }, observed);
    }

    [Fact]
    public void OnNext_LeavesAListenerForAnotherEventAlone()
    {
        var observed = new List<string>();
        var observer = BuildObserver();
        observer.SetEventListeners(new Dictionary<string, Func<HubObserveData<RoleDialogModel>, Task>>
        {
            [ChatEvent.OnReceiveLlmStreamMessage] = data =>
            {
                observed.Add(data.Data.Content);
                return Task.CompletedTask;
            }
        });

        observer.OnNext(BuildEvent(ChatEvent.OnIndicationReceived, "chunk"));

        Assert.Empty(observed);
    }

    private static ConversationObserver BuildObserver()
    {
        var conversation = new Mock<IConversationService>();
        conversation.SetupGet(x => x.ConversationId).Returns("conversation-1");

        var services = new ServiceCollection();
        services.AddSingleton(conversation.Object);
        services.AddSingleton(new Mock<IConversationStorage>().Object);
        services.AddSingleton(new Mock<IRoutingContext>().Object);

        return new ConversationObserver(services.BuildServiceProvider(), NullLogger<ConversationObserver>.Instance);
    }

    private static HubObserveData<RoleDialogModel> BuildEvent(string eventName, string content)
    {
        return new HubObserveData<RoleDialogModel>
        {
            EventName = eventName,
            RefId = "conversation-1",
            Data = new RoleDialogModel(AgentRole.Assistant, content) { Indication = content }
        };
    }
}
