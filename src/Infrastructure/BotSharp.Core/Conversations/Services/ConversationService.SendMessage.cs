using BotSharp.Abstraction.Agents.Models;
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
        Console.WriteLine(content, Color.OrangeRed);
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

        var ret = agentId == settings.RouterId ?
            await routing.InstructLoop(message) :
            await routing.ExecuteOnce(agent, message);

        await HandleAssistantMessage(message, onMessageReceived);

        var statistics = _services.GetRequiredService<ITokenStatistics>();
        statistics.PrintStatistics();

        routing.ResetRecursiveCounter();
        routing.RefreshDialogs();

        return ret;
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

    private async Task HandleAssistantMessage(RoleDialogModel message, Func<RoleDialogModel, Task> onMessageReceived)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.GetAgent(message.CurrentAgentId);
        var agentName = agent.Name;

        var text = message.Role == AgentRole.Function ?
            $"Sending [{agentName}] {message.FunctionName}: {message.Content}" :
            $"Sending [{agentName}] {message.Role}: {message.Content}";
#if DEBUG
        Console.WriteLine(text, Color.Yellow);
#else
        _logger.LogInformation(text);
#endif

        var hooks = _services.GetServices<IConversationHook>().ToList();
        foreach (var hook in hooks)
        {
            await hook.OnResponseGenerated(message);
        }

        await onMessageReceived(message);

        // Add to dialog history
        _storage.Append(_conversationId, message);
    }
}
