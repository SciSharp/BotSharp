using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Conversations.Settings;
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
        var db = _services.GetRequiredService<AgentDbContext>();
        var query = from sess in db.Conversation
                    where sess.Id == id
                    orderby sess.CreatedTime descending
                    select sess.ToConversation();
        return query.FirstOrDefault();
    }

    public async Task<List<Conversation>> GetConversations()
    {
        var db = _services.GetRequiredService<AgentDbContext>();
        var query = from sess in db.Conversation
                    where sess.UserId == _user.Id
                    orderby sess.CreatedTime descending
                    select sess.ToConversation();
        return query.ToList();
    }

    public async Task<Conversation> NewConversation(Conversation sess)
    {
        var db = _services.GetRequiredService<AgentDbContext>();

        var record = ConversationRecord.FromConversation(sess);
        record.Id = sess.Id.IfNullOrEmptyAs(Guid.NewGuid().ToString());
        record.UserId = sess.UserId.IfNullOrEmptyAs(_user.Id);
        record.Title = "New Conversation";

        db.Transaction<IAgentTable>(delegate
        {
            db.Add<IAgentTable>(record);
        });

        _storage.InitStorage(sess.AgentId, record.Id);

        return record.ToConversation();
    }

    public async Task<string> SendMessage(string agentId, string conversationId, RoleDialogModel lastDalog)
    {
        _storage.Append(agentId, conversationId, lastDalog);

        var wholeDialogs = GetDialogHistory(agentId, conversationId);

        var response = await SendMessage(agentId, conversationId, wholeDialogs);

        _storage.Append(agentId, conversationId, new RoleDialogModel("assistant", response));

        return response;
    }

    public async Task<string> SendMessage(string agentId, string conversationId, List<RoleDialogModel> wholeDialogs)
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

        // Before chat completion hook
        var hooks = _services.GetServices<IConversationCompletionHook>().ToList();

        hooks.ForEach(hook =>
        {
            hook.SetAgent(agent)
                .SetConversation(converation)
                .SetDialogs(wholeDialogs)
                .SetChatCompletion(chatCompletion)
                .BeforeCompletion();
        });
        
        var response = await chatCompletion.GetChatCompletionsStreamingAsync(agent, wholeDialogs);

        // After chat completion hook
        hooks.ForEach(async hook =>
        {
            response = await hook.AfterCompletion(response);
        });

        return response;
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

    public List<RoleDialogModel> GetDialogHistory(string agentId, string conversationId)
    {
        return _storage.GetDialogs(agentId, conversationId);
    }
}
