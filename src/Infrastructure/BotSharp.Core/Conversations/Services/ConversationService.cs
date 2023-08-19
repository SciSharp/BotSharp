using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.MLTasks;

namespace BotSharp.Core.Conversations.Services;

public class ConversationService : IConversationService
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _services;
    private readonly IUserIdentity _user;
    private readonly ConversationSetting _settings;
    private readonly IConversationStorage _storage;

    public ConversationService(IServiceProvider services,
        IUserIdentity user,
        ConversationSetting settings,
        IConversationStorage storage,
        ILogger<ConversationService> logger)
    {
        _services = services;
        _user = user;
        _settings = settings;
        _storage = storage;
        _logger = logger;
    }

    public Task DeleteConversation(string id)
    {
        throw new NotImplementedException();
    }

    public async Task<Conversation> GetConversation(string id)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var query = from sess in db.Conversation
                    where sess.Id == id
                    orderby sess.CreatedTime descending
                    select sess.ToConversation();
        return query.FirstOrDefault();
    }

    public async Task<List<Conversation>> GetConversations()
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var query = from sess in db.Conversation
                    where sess.UserId == _user.Id
                    orderby sess.CreatedTime descending
                    select sess.ToConversation();
        return query.ToList();
    }

    public async Task<Conversation> NewConversation(Conversation sess)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();

        var record = ConversationRecord.FromConversation(sess);
        record.Id = sess.Id.IfNullOrEmptyAs(Guid.NewGuid().ToString());
        record.UserId = sess.UserId.IfNullOrEmptyAs(_user.Id);
        record.Title = "New Conversation";

        db.Transaction<IBotSharpTable>(delegate
        {
            db.Add<IBotSharpTable>(record);
        });

        _storage.InitStorage(record.Id);

        return record.ToConversation();
    }

    public async Task<bool> SendMessage(string agentId, string conversationId,
        RoleDialogModel lastDialog,
        Func<RoleDialogModel, Task> onMessageReceived,
        Func<RoleDialogModel, Task> onFunctionExecuting)
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

        var router = _services.GetRequiredService<IAgentRouting>();
        var agent = await router.LoadCurrentAgent();

        _logger.LogInformation($"[{agent.Name}] {lastDialog.Role}: {lastDialog.Content}");

        lastDialog.CurrentAgentId = agent.Id;
        _storage.Append(conversationId, agent.Id, lastDialog);

        var wholeDialogs = GetDialogHistory(conversationId);

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

        var chatCompletion = GetChatCompletion();
        var result = await GetChatCompletionsAsyncRecursively(chatCompletion,
            conversationId,
            agent,
            wholeDialogs,
            onMessageReceived,
            onFunctionExecuting);

        return result;
    }

    private async Task<bool> GetChatCompletionsAsyncRecursively(IChatCompletion chatCompletion,
        string conversationId,
        Agent agent, 
        List<RoleDialogModel> wholeDialogs,
        Func<RoleDialogModel, Task> onMessageReceived,
        Func<RoleDialogModel, Task> onFunctionExecuting)
    {
        var result = await chatCompletion.GetChatCompletionsAsync(agent, wholeDialogs, async msg =>
        {
            await HandleAssistantMessage(msg, onMessageReceived);

            // Add to dialog history
            _storage.Append(conversationId, agent.Id, msg);
        }, async fn =>
        {
            var preAgentId = agent.Id;

            await HandleFunctionMessage(fn, onFunctionExecuting);

            fn.Content = fn.ExecutionResult;

            // Agent has been transferred
            if (fn.CurrentAgentId != preAgentId)
            {
                var agentSettings = _services.GetRequiredService<AgentSettings>();
                var agentService = _services.GetRequiredService<IAgentService>();
                agent = await agentService.LoadAgent(fn.CurrentAgentId);

                // Set state to make next conversation will go to this agent directly
                // var state = _services.GetRequiredService<IConversationStateService>();
                // state.SetState("agentId", fn.CurrentAgentId);
            }

            // Add to dialog history
            _storage.Append(conversationId, preAgentId, fn);

            // After function is executed, pass the result to LLM to get a natural response
            wholeDialogs.Add(fn);

            await GetChatCompletionsAsyncRecursively(chatCompletion, conversationId, agent, wholeDialogs, onMessageReceived, onFunctionExecuting);
        });

        return result;
    }

    private async Task HandleAssistantMessage(RoleDialogModel msg, Func<RoleDialogModel, Task> onMessageReceived)
    {
        var hooks = _services.GetServices<IConversationHook>().ToList();

        // After chat completion hook
        foreach (var hook in hooks)
        {
            await hook.AfterCompletion(msg);
        }

        await onMessageReceived(msg);
    }

    private async Task HandleFunctionMessage(RoleDialogModel msg, Func<RoleDialogModel, Task> onFunctionExecuting)
    {
        // Save states
        SaveStateByArgs(msg.FunctionArgs);

        // Call functions
        await onFunctionExecuting(msg);
        await CallFunctions(msg);
    }

    private void SaveStateByArgs(string args)
    {
        var stateService = _services.GetRequiredService<IConversationStateService>();
        var jo = JsonSerializer.Deserialize<object>(args);
        if (jo is JsonElement root)
        {
            foreach (JsonProperty property in root.EnumerateObject())
            {
                stateService.SetState(property.Name, property.Value.ToString());
            }
        }
    }

    private async Task CallFunctions(RoleDialogModel msg)
    {
        var hooks = _services.GetServices<IConversationHook>().ToList();

        // Invoke functions
        var functions = _services.GetServices<IFunctionCallback>()
            .Where(x => x.Name == msg.FunctionName)
            .ToList();

        if (functions.Count == 0)
        {
            _logger.LogError($"Can't find function implementation of {msg.FunctionName}.");
            return;
        }

        foreach (var fn in functions)
        {
            // Before executing functions
            foreach (var hook in hooks)
            {
                await hook.OnFunctionExecuting(msg);
            }

            // Execute function
            await fn.Execute(msg);

            // After functions have been executed
            foreach (var hook in hooks)
            {
                await hook.OnFunctionExecuted(msg);
            }
        }
    }

    public IChatCompletion GetChatCompletion()
    {
        var completions = _services.GetServices<IChatCompletion>();
        return completions.FirstOrDefault(x => x.GetType().FullName.EndsWith(_settings.ChatCompletion));
    }

    public Task CleanHistory(string agentId)
    {
        throw new NotImplementedException();
    }

    public List<RoleDialogModel> GetDialogHistory(string conversationId, int lastCount = 20)
    {
        var dialogs = _storage.GetDialogs(conversationId);
        return dialogs
            .Where(x => x.CreatedAt > DateTime.UtcNow.AddHours(-8))
            .TakeLast(lastCount).ToList();
    }
}
