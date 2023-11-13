using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Messaging.Models;
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

        _storage.Append(_conversationId, message);

        var hooks = _services.GetServices<IConversationHook>().ToList();

        // Before chat completion hook
        foreach (var hook in hooks)
        {
            hook.SetAgent(agent)
                .SetConversation(conversation);

            await hook.OnMessageReceived(message);

            // Interrupted by hook
            if (message.StopCompletion)
            {
                await onMessageReceived(message);
                _storage.Append(_conversationId, message);
                return true;
            }
        }

        // Routing with reasoning
        var routing = _services.GetRequiredService<IRoutingService>();
        var settings = _services.GetRequiredService<RoutingSettings>();

        var response = agentId == settings.RouterId ?
            await routing.InstructLoop(message) :
            await routing.ExecuteOnce(agent, message);

        await HandleAssistantMessage(response, onMessageReceived);

        var statistics = _services.GetRequiredService<ITokenStatistics>();
        statistics.PrintStatistics();

        routing.ResetRecursiveCounter();

        return true;
    }

    private async Task<Conversation> GetConversationRecord(string agentId)
    {
        var converation = await GetConversation(_conversationId);

        // Create conversation if this conversation not exists
        if (converation == null)
        {
            var sess = new Conversation
            {
                Id = _conversationId,
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

        var text = response.Role == AgentRole.Function ?
            $"Sending [{agentName}] {response.FunctionName}: {response.Content}" :
            $"Sending [{agentName}] {response.Role}: {response.Content}";
#if DEBUG
        Console.WriteLine(text, Color.Yellow);
#else
        _logger.LogInformation(text);
#endif

        // Process rich content
        if (response.RichContent != null &&
            response.RichContent is RichContent<IMessageTemplate> template &&
            string.IsNullOrEmpty(template.Message.Text))
        {
            template.Message.Text = response.Content;
        }

        // Only read content from RichContent for UI rendering. When richContent is null, create a basic text message for richContent.
        var state = _services.GetRequiredService<IConversationStateService>();
        response.RichContent = response.RichContent ?? new RichContent<IMessageTemplate>
        {
            Recipient = new Recipient { Id = state.GetConversationId() },
            Message = new TextMessage { Text = response.Content }
        };

        var hooks = _services.GetServices<IConversationHook>().ToList();
        foreach (var hook in hooks)
        {
            await hook.OnResponseGenerated(response);
        }

        await onMessageReceived(response);

        // Add to dialog history
        _storage.Append(_conversationId, response);
    }
}
