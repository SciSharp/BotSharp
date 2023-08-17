using Amazon.SecurityToken.Model.Internal.MarshallTransformations;
using BotSharp.Abstraction.Agents.Enums;
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
        RoleDialogModel lastDalog,
        Func<RoleDialogModel, Task> onMessageReceived,
        Func<RoleDialogModel, Task> onFunctionExecuting)
    {
        lastDalog.CurrentAgentId = agentId;
        _storage.Append(conversationId, lastDalog);

        var wholeDialogs = GetDialogHistory(conversationId);

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
        var result = await chatCompletion.GetChatCompletionsAsync(agent, wholeDialogs, async msg =>
        {
            await HandleMessage(conversationId, agent, msg, onMessageReceived, onFunctionExecuting);

            if (msg.NeedReloadAgent)
            {
                await HandleMessageIfAgentReloaded(conversationId, agent, msg, wholeDialogs, onMessageReceived, onFunctionExecuting);
            }
        });

        return result;
    }

    private async Task HandleMessage(string conversationId, Agent agent, RoleDialogModel msg,
        Func<RoleDialogModel, Task> onMessageReceived,
        Func<RoleDialogModel, Task> onFunctionExecuting)
    {
        if (msg.Role == "function")
        {
            // Save states
            SaveStateByArgs(msg.FunctionArgs);

            // Call functions
            await onFunctionExecuting(msg);
            await CallFunctions(msg);

            // Add to dialog history
            if (msg.ExecutionResult != null)
            {
                if (msg.NeedReloadAgent)
                {
                    _logger.LogInformation($"Skipped append dialog log: {msg.FunctionName}\n{msg.FunctionArgs}\n{msg.ExecutionResult}");
                    return;
                }

                _storage.Append(conversationId, new RoleDialogModel(msg.Role, msg.Content)
                {
                    CurrentAgentId = agent.Id,
                    FunctionName = msg.FunctionName,
                    FunctionArgs = msg.FunctionArgs,
                    ExecutionResult = msg.ExecutionResult
                });
            }
        }
        else
        {
            // Add to dialog history
            _storage.Append(conversationId, new RoleDialogModel(msg.Role, msg.Content)
            {
                CurrentAgentId = agent.Id
            });

            var hooks = _services.GetServices<IConversationHook>().ToList();
            // After chat completion hook
            foreach (var hook in hooks)
            {
                await hook.AfterCompletion(msg);
            }

            await onMessageReceived(msg);
        }
    }

    private async Task HandleMessageIfAgentReloaded(string conversationId, Agent agent, 
        RoleDialogModel msg, 
        List<RoleDialogModel> wholeDialogs,
        Func<RoleDialogModel, Task> onMessageReceived,
        Func<RoleDialogModel, Task> onFunctionExecuting)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        var currentAgentId = state.GetState("agentId");

        // Send to LLM to get final response when agent is switched.
        var conv = _services.GetRequiredService<IConversationService>();
        var chatCompletion = conv.GetChatCompletion();
        var agentService = _services.GetRequiredService<IAgentService>();
        var newAgent = await agentService.LoadAgent(currentAgentId);
        await chatCompletion.GetChatCompletionsAsync(newAgent, wholeDialogs, async newMsg =>
        {
            if (newMsg.Role == AgentRole.Function)
            {
                await HandleMessage(conversationId, agent, newMsg, onMessageReceived, onFunctionExecuting);
            }
            else
            {
                msg.StopPropagate = true;
                await onMessageReceived(newMsg);

                _storage.Append(conversationId, new RoleDialogModel(newMsg.Role, newMsg.Content)
                {
                    CurrentAgentId = agent.Id
                });
            }
        });
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
