using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Knowledges.Models;
using BotSharp.Abstraction.MLTasks;

namespace BotSharp.Core.Conversations.Services;

public class ConversationService : IConversationService
{
    private readonly IServiceProvider _services;
    private readonly IUserIdentity _user;
    private readonly ConversationSetting _settings;
    private readonly IConversationStorage _storage;

    public ConversationService(IServiceProvider services, 
        IUserIdentity user,
        ConversationSetting settings,
        IConversationStorage storage)
    {
        _services = services;
        _user = user;
        _settings = settings;
        _storage = storage;
    }

    public Task DeleteConversation(string id)
    {
        throw new NotImplementedException();
    }

    public async Task<Conversation> GetConversation(string id)
    {
        var db = _services.GetRequiredService<BotSharpDbContext>();
        var query = from sess in db.Conversation
                    where sess.Id == id
                    orderby sess.CreatedTime descending
                    select sess.ToConversation();
        return query.FirstOrDefault();
    }

    public async Task<List<Conversation>> GetConversations()
    {
        var db = _services.GetRequiredService<BotSharpDbContext>();
        var query = from sess in db.Conversation
                    where sess.UserId == _user.Id
                    orderby sess.CreatedTime descending
                    select sess.ToConversation();
        return query.ToList();
    }

    public async Task<Conversation> NewConversation(Conversation sess)
    {
        var db = _services.GetRequiredService<BotSharpDbContext>();

        var record = ConversationRecord.FromConversation(sess);
        record.Id = sess.Id.IfNullOrEmptyAs(Guid.NewGuid().ToString());
        record.UserId = sess.UserId.IfNullOrEmptyAs(_user.Id);
        record.Title = "New Conversation";

        db.Transaction<IBotSharpTable>(delegate
        {
            db.Add<IBotSharpTable>(record);
        });

        _storage.InitStorage(sess.AgentId, record.Id);

        return record.ToConversation();
    }

    public async Task<bool> SendMessage(string agentId, string conversationId, RoleDialogModel lastDalog, 
        Func<RoleDialogModel, Task> onMessageReceived, 
        Func<RoleDialogModel, Task> onFunctionExecuting)
    {
        _storage.Append(agentId, conversationId, lastDalog);

        var wholeDialogs = GetDialogHistory(agentId, conversationId);

        var response = await SendMessage(agentId, conversationId, wholeDialogs, async msg =>
        {
            if (msg.Role == "function")
            {
                // Invoke functions
                var functions = _services.GetServices<IFunctionCallback>().Where(x => x.Name == msg.FunctionName);
                foreach (var fn in functions)
                {
                    await onFunctionExecuting(msg);

                    msg.ExecutionResult = await fn.Execute(msg.Content);

                    var result = msg.ExecutionResult.Replace("\r", " ").Replace("\n", " ");
                    var content = $"{result}";
                    // Console.WriteLine($"{msg.Role}: {content}");
                    _storage.Append(agentId, conversationId, new RoleDialogModel(msg.Role, content)
                    {
                        FunctionName = msg.FunctionName,
                    });
                }
            }
            else
            {
                var content = msg.Content.Replace("\r", " ").Replace("\n", " ");
                // Console.WriteLine($"{msg.Role}: {content}");
                _storage.Append(agentId, conversationId, new RoleDialogModel(msg.Role, content));
                
                await onMessageReceived(msg);
            }
        });

        return response;
    }

    public async Task<bool> SendMessage(string agentId, string conversationId, List<RoleDialogModel> wholeDialogs, Func<RoleDialogModel, Task> onMessageReceived)
    {
        var agent = await _services.GetRequiredService<IAgentService>().GetAgent(agentId);
        var converation = await GetConversation(conversationId);

        // Get relevant domain knowledge
        if (_settings.EnableKnowledgeBase)
        {
            var knowledge = _services.GetRequiredService<IKnowledgeService>();
            agent.Knowledges = await knowledge.GetKnowledges(new KnowledgeRetrievalModel
            {
                AgentId = agentId,
                Question = string.Join("\n", wholeDialogs.Select(x => x.Content))
            });
        }

        var chatCompletion = GetChatCompletion();

        var hooks = _services.GetServices<IConversationCompletionHook>().ToList();

        // Before chat completion hook
        foreach (var hook in hooks)
        {
            await hook.SetAgent(agent)
                .SetConversation(converation)
                .SetDialogs(wholeDialogs)
                .SetChatCompletion(chatCompletion)
                .BeforeCompletion();
        }

        var result = await chatCompletion.GetChatCompletionsAsync(agent, wholeDialogs, async msg =>
        {
            if (msg.Role == "function")
            {
                // Before executing functions
                foreach (var hook in hooks)
                {
                    await hook.OnFunctionExecuting(msg.FunctionName, msg.Content);
                }
            }
            else
            {
                // After chat completion hook
                foreach (var hook in hooks)
                {
                    await hook.AfterCompletion(msg);
                }
            }
            await onMessageReceived(msg);
        });

        return result;
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

    public List<RoleDialogModel> GetDialogHistory(string agentId, string conversationId, int lastCount = 20)
    {
        var dialogs = _storage.GetDialogs(agentId, conversationId);
        return dialogs.TakeLast(lastCount).ToList();
    }
}
