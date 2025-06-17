using BotSharp.Abstraction.Hooks;
using BotSharp.Abstraction.Infrastructures.Enums;
using BotSharp.Abstraction.Routing.Enums;
using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Conversations.Services;

public partial class ConversationService
{
    public async Task<bool> StreamMessage(string agentId,
        RoleDialogModel message,
        PostbackMessageModel? replyMessage)
    {
        var conversation = await GetConversationRecordOrCreateNew(agentId);
        var agentService = _services.GetRequiredService<IAgentService>();
        Agent agent = await agentService.LoadAgent(agentId);

        var content = $"Received [{agent.Name}] {message.Role}: {message.Content}";
        _logger.LogInformation(content);

        message.CurrentAgentId = agent.Id;
        if (string.IsNullOrEmpty(message.SenderId))
        {
            message.SenderId = _user.Id;
        }

        var conv = _services.GetRequiredService<IConversationService>();
        var dialogs = conv.GetDialogHistory();

        var statistics = _services.GetRequiredService<ITokenStatistics>();

        RoleDialogModel response = message;
        bool stopCompletion = false;

        // Enqueue receiving agent first in case it stop completion by OnMessageReceived
        var routing = _services.GetRequiredService<IRoutingService>();
        routing.Context.SetMessageId(_conversationId, message.MessageId);

        // Save payload in order to assign the payload before hook is invoked
        if (replyMessage != null && !string.IsNullOrEmpty(replyMessage.Payload))
        {
            message.Payload = replyMessage.Payload;
        }

        var hooks = _services.GetHooksOrderByPriority<IConversationHook>(message.CurrentAgentId);
        foreach (var hook in hooks)
        {
            hook.SetAgent(agent)
                .SetConversation(conversation);

            if (replyMessage == null || string.IsNullOrEmpty(replyMessage.FunctionName))
            {
                await hook.OnMessageReceived(message);
            }
            else
            {
                await hook.OnPostbackMessageReceived(message, replyMessage);
            }

            // Interrupted by hook
            if (message.StopCompletion)
            {
                stopCompletion = true;
                routing.Context.Pop();
                break;
            }
        }

        if (!stopCompletion)
        {
            // Routing with reasoning
            var settings = _services.GetRequiredService<RoutingSettings>();

            // reload agent in case it has been changed by hook
            if (message.CurrentAgentId != agent.Id)
            {
                agent = await agentService.LoadAgent(message.CurrentAgentId);
            }

            await routing.InstructStream(agent, message, dialogs);
            routing.Context.ResetRecursiveCounter();
        }

        return true;
    }
}
