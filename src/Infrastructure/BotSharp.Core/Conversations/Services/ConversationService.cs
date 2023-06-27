using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Conversations.Settings;
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
        record.Id = Guid.NewGuid().ToString();
        record.UserId = _user.Id;
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

        _storage.Append(agentId, conversationId, new RoleDialogModel
        {
            Role = "assistant",
            Text = response
        });

        return response;
    }

    public async Task<string> SendMessage(string agentId, string conversationId, List<RoleDialogModel> wholeDialogs)
    {
        var agent = await _services.GetRequiredService<IAgentService>().GetAgent(agentId);
        var chat = GetChatCompletion();
        var response = await chat.GetChatCompletionsAsync(agent, wholeDialogs);
        
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
