using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Conversations.Services;

public partial class ConversationService
{
    public async Task<bool> SendMessage(string agentId, 
        RoleDialogModel lastDialog,
        Func<RoleDialogModel, Task> onMessageReceived,
        Func<RoleDialogModel, Task> onFunctionExecuting,
        Func<RoleDialogModel, Task> onFunctionExecuted)
    {
        var conversation = await GetConversationRecord(agentId);

        var agentService = _services.GetRequiredService<IAgentService>();
        Agent agent = await agentService.LoadAgent(agentId);

        _logger.LogInformation($"[{agent.Name}] {lastDialog.Role}: {lastDialog.Content}");

        lastDialog.CurrentAgentId = agent.Id;
        
        var wholeDialogs = GetDialogHistory();
        wholeDialogs.Add(lastDialog);

        _storage.Append(_conversationId, lastDialog);

        var hooks = _services.GetServices<IConversationHook>().ToList();

        // Before chat completion hook
        foreach (var hook in hooks)
        {
            hook.SetAgent(agent)
                .SetConversation(conversation);

            await hook.OnDialogsLoaded(wholeDialogs);
            await hook.BeforeCompletion(lastDialog);

            // Interrupted by hook
            if (lastDialog.StopCompletion)
            {
                var message = new RoleDialogModel(AgentRole.Assistant, lastDialog.Content)
                {
                    CurrentAgentId = agent.Id
                };
                await onMessageReceived(message);
                _storage.Append(_conversationId, message);
                return true;
            }
        }

        // Routing with reasoning
        var settings = _services.GetRequiredService<RoutingSettings>();
        if (settings.RouterId == agent.Id)
        {
            var routing = _services.GetRequiredService<IRoutingService>();
            var reasonedContext = await routing.Enter(agent, wholeDialogs);

            if (reasonedContext.FunctionName == "interrupt_task_execution")
            {
                await HandleAssistantMessage(agent, new RoleDialogModel(AgentRole.Assistant, reasonedContext.Content)
                {
                    CurrentAgentId = agent.Id
                }, onMessageReceived);

                return true;
            }
            else if (reasonedContext.FunctionName == "response_to_user")
            {
                await HandleAssistantMessage(agent, new RoleDialogModel(AgentRole.Assistant, reasonedContext.Content)
                {
                    CurrentAgentId = agent.Id
                }, onMessageReceived);

                return true;
            }
            else if (reasonedContext.FunctionName == "continue_execute_task")
            {
                if (reasonedContext.CurrentAgentId != agent.Id)
                {
                    agent = await agentService.LoadAgent(reasonedContext.CurrentAgentId);
                }
            }
            else if (reasonedContext.FunctionName == "route_to_agent")
            {
                if (reasonedContext.CurrentAgentId != agent.Id)
                {
                    agent = await agentService.LoadAgent(reasonedContext.CurrentAgentId);
                }
            }

            routing.Dialogs.ForEach(x => 
            {
                wholeDialogs.Add(x);
                if (x.Content != null)
                {
                    _storage.Append(_conversationId, x);
                }
            });
        }

        var result = await GetChatCompletionsAsyncRecursively(agent,
            wholeDialogs,
            onMessageReceived,
            onFunctionExecuting,
            onFunctionExecuted);

        return result;
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

    private void SaveStateByArgs(string args)
    {
        var stateService = _services.GetRequiredService<IConversationStateService>();
        var jo = JsonSerializer.Deserialize<object>(args);
        if (jo is JsonElement root)
        {
            foreach (JsonProperty property in root.EnumerateObject())
            {
                if (!string.IsNullOrEmpty(property.Value.ToString()))
                {
                    stateService.SetState(property.Name, property.Value);
                }
            }
        }
    }
}
