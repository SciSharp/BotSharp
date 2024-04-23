using BotSharp.Abstraction.Messaging;
using BotSharp.Abstraction.Messaging.Models.RichContent;
using BotSharp.Abstraction.Routing.Settings;
using System.Drawing;

namespace BotSharp.Core.Conversations.Services;

public partial class ConversationService
{
    public async Task<bool> SendMessage(string agentId,
        RoleDialogModel message,
        PostbackMessageModel? replyMessage,
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
        message.CreatedAt = DateTime.UtcNow;
        if (string.IsNullOrEmpty(message.SenderId))
        {
            message.SenderId = _user.Id;
        }

        var conv = _services.GetRequiredService<IConversationService>();
        var dialogs = conv.GetDialogHistory();

        var statistics = _services.GetRequiredService<ITokenStatistics>();
        var hooks = _services.GetServices<IConversationHook>().ToList();

        RoleDialogModel response = message;
        bool stopCompletion = false;

        // Enqueue receiving agent first in case it stop completion by OnMessageReceived
        var routing = _services.GetRequiredService<IRoutingService>();
        routing.Context.SetMessageId(_conversationId, message.MessageId);
        routing.Context.Push(agent.Id);

        // Before chat completion hook
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

            response = agent.Type == AgentType.Routing ?
                await routing.InstructLoop(message, dialogs) :
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

    private async Task HandleAssistantMessage(RoleDialogModel response, Func<RoleDialogModel, Task> onResponseReceived)
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

        // Patch return function name
        if (response.PostbackFunctionName != null)
        {
            response.FunctionName = response.PostbackFunctionName;
        }

        if (response.Instruction != null)
        {
            var conversation = _services.GetRequiredService<IConversationService>();
            var updatedConversation = await conversation.UpdateConversationTitle(_conversationId, response.Instruction.NextActionReason);

            // Emit conversation task completed hook
            if (response.Instruction.TaskCompleted)
            {
                await HookEmitter.Emit<IConversationHook>(_services, async hook =>
                    await hook.OnTaskCompleted(response)
                );
            }

            // Emit conversation ending hook
            if (response.Instruction.ConversationEnd)
            {
                await HookEmitter.Emit<IConversationHook>(_services, async hook =>
                    await hook.OnConversationEnding(response)
                );
            }
        }

        await HookEmitter.Emit<IConversationHook>(_services, async hook =>
            await hook.OnResponseGenerated(response)
        );

        await onResponseReceived(response);

        // Add to dialog history
        _storage.Append(_conversationId, response);
    }
}
