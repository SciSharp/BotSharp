using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Messaging;
using BotSharp.Abstraction.Messaging.Models.RichContent;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Settings;
using System.Drawing;

namespace BotSharp.Core.Conversations.Services;

public partial class ConversationService
{
    public async Task<bool> SendMessage(string agentId,
        RoleDialogModel message,
        Func<RoleDialogModel, Task> onMessageReceived,
        Func<RoleDialogModel, Task> onFunctionExecuting,
        Func<RoleDialogModel, Task> onFunctionExecuted)
    {
        var conversation = await GetConversationRecord(agentId);

        var agentService = _services.GetRequiredService<IAgentService>();
        Agent agent = await agentService.LoadAgent(agentId);

        var content = $"Received [{agent.Name}] {message.Role}: {message.Content}";
#if DEBUG
        Console.WriteLine(content, Color.GreenYellow);
#else
        _logger.LogInformation(content);
#endif

        message.CurrentAgentId = agent.Id;
        message.SenderId = _user.Id;

        _storage.Append(_conversationId, message);

        var statistics = _services.GetRequiredService<ITokenStatistics>();
        var hooks = _services.GetServices<IConversationHook>().ToList();

        RoleDialogModel response = message;
        bool stopCompletion = false;

        // Before chat completion hook
        foreach (var hook in hooks)
        {
            hook.SetAgent(agent)
                .SetConversation(conversation);

            await hook.OnMessageReceived(message);

            // Interrupted by hook
            if (message.StopCompletion)
            {
                stopCompletion = true;
            }
        }

        if (!stopCompletion)
        {
            // Routing with reasoning
            var routing = _services.GetRequiredService<IRoutingService>();
            var settings = _services.GetRequiredService<RoutingSettings>();

            response = settings.AgentIds.Contains(agentId) ?
                await routing.InstructLoop(message) :
                await routing.InstructDirect(agent, message);

            routing.ResetRecursiveCounter();
        }

        await HandleAssistantMessage(response, onMessageReceived);
        statistics.PrintStatistics();

        return true;
    }

    private async Task<Conversation> GetConversationRecord(string agentId)
    {
        var converation = await GetConversation(_conversationId);

        // Create conversation if this conversation does not exist
        if (converation == null)
        {
            var state = _services.GetRequiredService<IConversationStateService>();
            var channel = state.GetState("channel");
            var sess = new Conversation
            {
                Id = _conversationId,
                Channel = channel,
                AgentId = agentId
            };
            converation = await NewConversation(sess);
        }

        return converation;
    }

    private async Task HandleAssistantMessage(RoleDialogModel response, Func<RoleDialogModel, Task> onMessageReceived)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.GetAgent(response.CurrentAgentId);
        var agentName = agent.Name;

        // Send message always in assistant role
        response.Role = AgentRole.Assistant;
        var text = $"Sending [{agentName}] {response.Role}: {response.Content}";
#if DEBUG
        Console.WriteLine(text, Color.Yellow);
#else
        _logger.LogInformation(text);
#endif

        // Process rich content
        if (response.RichContent != null &&
            response.RichContent is RichContent<IRichMessage> template &&
            string.IsNullOrEmpty(template.Message.Text))
        {
            template.Message.Text = response.Content;
        }

        // Only read content from RichContent for UI rendering. When richContent is null, create a basic text message for richContent.
        var state = _services.GetRequiredService<IConversationStateService>();
        response.RichContent = response.RichContent ?? new RichContent<IRichMessage>
        {
            Recipient = new Recipient { Id = state.GetConversationId() },
            Message = new TextMessage(response.Content)
        };

        var hooks = _services.GetServices<IConversationHook>().ToList();
        foreach (var hook in hooks)
        {
            await hook.OnResponseGenerated(response);
        }

        await onMessageReceived(response);

        // Add to dialog history
        _storage.Append(_conversationId, response);

        if (response.Instruction != null)
        {
            var conversation = _services.GetRequiredService<IConversationService>();
            var updatedConversation = await conversation.UpdateConversationTitle(_conversationId, response.Instruction.Reason);
        }
    }
}
