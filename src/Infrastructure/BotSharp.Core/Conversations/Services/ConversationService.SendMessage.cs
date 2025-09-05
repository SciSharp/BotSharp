using BotSharp.Abstraction.Infrastructures.Enums;
using BotSharp.Abstraction.Messaging;
using BotSharp.Abstraction.Messaging.Models.RichContent;
using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Conversations.Services;

public partial class ConversationService
{
    public async Task<bool> SendMessage(string agentId,
        RoleDialogModel message,
        PostbackMessageModel? replyMessage,
        Func<RoleDialogModel, Task> onMessageReceived)
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

            if (agent.Type == AgentType.Routing)
            {
                // Check the routing mode
                var states = _services.GetRequiredService<IConversationStateService>();
                var routingMode = states.GetState(StateConst.ROUTING_MODE, RoutingMode.Eager);
                routing.Context.Push(agent.Id, reason: "request started", updateLazyRouting: false);

                if (routingMode == RoutingMode.Lazy)
                {
                    message.CurrentAgentId = states.GetState(StateConst.LAZY_ROUTING_AGENT_ID, message.CurrentAgentId);
                    routing.Context.Push(message.CurrentAgentId, reason: "lazy routing", updateLazyRouting: false);
                }

                response = await routing.InstructLoop(agent, message, dialogs);
            }
            else
            {
                response = await routing.InstructDirect(agent, message, dialogs);
            }

            routing.Context.ResetRecursiveCounter();
        }

        await HandleAssistantMessage(response, onMessageReceived);
        statistics.PrintStatistics();

        return true;
    }

    private async Task HandleAssistantMessage(RoleDialogModel response, Func<RoleDialogModel, Task> onResponseReceived)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.GetAgent(response.CurrentAgentId);
        var agentName = agent.Name;

        // Send message always in assistant role
        response.Role = AgentRole.Assistant;
        var text = $"Sending [{agentName}] {response.Role}: {response.Content}";
#if DEBUG
        Console.WriteLine(text);
#else
        _logger.LogInformation(text);
#endif

        // Process rich content
        if (response.RichContent != null &&
            response.RichContent is RichContent<IRichMessage> template &&
            string.IsNullOrEmpty(template.Message.Text))
        {
            template.Message.Text = response.SecondaryContent ?? response.Content;
        }

        // Only read content from RichContent for UI rendering. When richContent is null, create a basic text message for richContent.
        var state = _services.GetRequiredService<IConversationStateService>();
        response.RichContent = response.RichContent ?? new RichContent<IRichMessage>
        {
            Recipient = new Recipient { Id = state.GetConversationId() },
            Message = new TextMessage(response.SecondaryContent ?? response.Content)
        };

        // Use model refined response
        if (string.IsNullOrEmpty(response.RichContent.Message.Text))
        {
            response.RichContent.Message.Text = response.Content;
        }

        // Patch return function name
        if (response.PostbackFunctionName != null)
        {
            response.FunctionName = response.PostbackFunctionName;
        }

        if (response.Instruction != null)
        {
            var conversation = _services.GetRequiredService<IConversationService>();
            var updatedConversation = await conversation.UpdateConversationTitle(_conversationId, response.Instruction.NextActionReason);

            // Emit conversation ending hook
            if (response.Instruction.ConversationEnd)
            {
                await HookEmitter.Emit<IConversationHook>(_services, async hook => await hook.OnConversationEnding(response),
                    response.CurrentAgentId);
                response.FunctionName = "conversation_end";
            }
        }

        await HookEmitter.Emit<IConversationHook>(_services, async hook => await hook.OnResponseGenerated(response),
            response.CurrentAgentId);

        await onResponseReceived(response);

        // Add to dialog history
        _storage.Append(_conversationId, response);
    }
}
