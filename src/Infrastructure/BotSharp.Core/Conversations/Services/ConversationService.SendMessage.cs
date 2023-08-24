using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.MLTasks;

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

        var router = _services.GetRequiredService<IAgentRouting>();
        var agent = await router.LoadRouter();

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

        var agentSettings = _services.GetRequiredService<AgentSettings>();

        var chatCompletion = GetChatCompletion();
        var result = await GetChatCompletionsAsyncRecursively(chatCompletion,
            conversationId,
            agent,
            wholeDialogs,
            agentSettings.MaxRecursiveDepth,
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
}
