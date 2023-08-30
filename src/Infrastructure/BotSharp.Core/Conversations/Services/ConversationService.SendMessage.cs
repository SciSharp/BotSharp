using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Routing.Settings;
using BotSharp.Core.Routing;

namespace BotSharp.Core.Conversations.Services;

public partial class ConversationService
{
    public async Task<bool> SendMessage(string agentId, string conversationId,
        RoleDialogModel lastDialog,
        Func<RoleDialogModel, Task> onMessageReceived,
        Func<RoleDialogModel, Task> onFunctionExecuting,
        Func<RoleDialogModel, Task> onFunctionExecuted)
    {
        var converation = await GetConversation(conversationId);

        // Create conversation if this conversation not exists
        if (converation == null)
        {
            var sess = new Conversation
            {
                Id = conversationId,
                AgentId = agentId
            };
            converation = await NewConversation(sess);
        }

        // conversation state
        var stateService = _services.GetRequiredService<IConversationStateService>();
        stateService.SetConversation(conversationId);
        stateService.Load();
        stateService.SetState("channel", lastDialog.Channel);

        var agentService = _services.GetRequiredService<IAgentService>();
        Agent agent = await agentService.LoadAgent(agentId);

        _logger.LogInformation($"[{agent.Name}] {lastDialog.Role}: {lastDialog.Content}");

        lastDialog.CurrentAgentId = agent.Id;
        
        var wholeDialogs = GetDialogHistory(conversationId);
        wholeDialogs.Add(lastDialog);

        _storage.Append(conversationId, agent.Id, lastDialog);

        // Get relevant domain knowledge
        /*if (_settings.EnableKnowledgeBase)
        {
            var knowledge = _services.GetRequiredService<IKnowledgeService>();
            agent.Knowledges = await knowledge.GetKnowledges(new KnowledgeRetrievalModel
            {
                AgentId = agentId,
                Question = string.Join("\n", wholeDialogs.Select(x => x.Content))
            });
        }*/

        var hooks = _services.GetServices<IConversationHook>().ToList();

        // Before chat completion hook
        foreach (var hook in hooks)
        {
            hook.SetAgent(agent)
                .SetConversation(converation);

            await hook.OnDialogsLoaded(wholeDialogs);
            await hook.BeforeCompletion();
        }

        // reasoning
        var settings = _services.GetRequiredService<RoutingSettings>();
        if (settings.ReasonerId == agent.Id)
        {
            var simulator = _services.GetRequiredService<Simulator>();
            var reasonedContext = await simulator.Enter(agent, wholeDialogs);

            if (reasonedContext.FunctionName == "interrupt_task_execution")
            {
                await HandleAssistantMessage(new RoleDialogModel(AgentRole.Assistant, reasonedContext.Content)
                {
                    CurrentAgentId = agent.Id,
                    Channel = lastDialog.Channel
                }, onMessageReceived);
                return true;
            }
            else if (reasonedContext.FunctionName == "response_to_user")
            {
                await HandleAssistantMessage(new RoleDialogModel(AgentRole.Assistant, reasonedContext.Content)
                {
                    CurrentAgentId = agent.Id,
                    Channel = lastDialog.Channel
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

            simulator.Dialogs.ForEach(x => 
            {
                wholeDialogs.Add(x);
                _storage.Append(conversationId, agent.Id, x);
            });
        }

        var chatCompletion = GetChatCompletion();
        var result = await GetChatCompletionsAsyncRecursively(chatCompletion,
            conversationId,
            agent,
            wholeDialogs,
            onMessageReceived,
            onFunctionExecuting,
            onFunctionExecuted);

        return result;
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
                    stateService.SetState(property.Name, property.Value.ToString());
                }
            }
        }
    }

    public IChatCompletion GetChatCompletion()
    {
        var completions = _services.GetServices<IChatCompletion>();
        return completions.FirstOrDefault(x => x.GetType().FullName.EndsWith(_settings.ChatCompletion));
    }

    public IChatCompletion GetGpt4ChatCompletion()
    {
        var completions = _services.GetServices<IChatCompletion>();
        return completions.FirstOrDefault(x => x.GetType().FullName.EndsWith("GPT4CompletionProvider"));
    }
}
