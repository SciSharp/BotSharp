using BotSharp.Abstraction.Repositories;

namespace BotSharp.Core.Conversations.Services;

public partial class ConversationService : IConversationService
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _services;
    private readonly IUserIdentity _user;
    private readonly ConversationSetting _settings;
    private readonly IConversationStorage _storage;
    private readonly IConversationStateService _state;
    private string _conversationId;
    public string ConversationId => _conversationId;

    public IConversationStateService States => _state;

    public ConversationService(
        IServiceProvider services,
        IUserIdentity user,
        ConversationSetting settings,
        IConversationStorage storage,
        IConversationStateService state,
        ILogger<ConversationService> logger)
    {
        _services = services;
        _user = user;
        _settings = settings;
        _storage = storage;
        _state = state;
        _logger = logger;
    }

    public Task DeleteConversation(string id)
    {
        throw new NotImplementedException();
    }

    public async Task<Conversation> GetConversation(string id)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var conversation = db.GetConversation(id);
        return conversation;
    }

    public async Task<List<Conversation>> GetConversations()
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var user = db.GetUserByExternalId(_user.Id);
        var conversations = db.GetConversations(user?.Id);
        return conversations.OrderByDescending(x => x.CreatedTime).ToList();
    }

    public async Task<Conversation> NewConversation(Conversation sess)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var user = db.GetUserByExternalId(_user.Id);
        var foundUserId = user?.Id ?? string.Empty;

        var record = sess;
        record.Id = sess.Id.IfNullOrEmptyAs(Guid.NewGuid().ToString());
        record.UserId = sess.UserId.IfNullOrEmptyAs(foundUserId);
        record.Title = "New Conversation";

        db.CreateNewConversation(record);

        var hooks = _services.GetServices<IConversationHook>().ToList();
        foreach (var hook in hooks)
        {
            await hook.OnConversationInitialized(record);
        }

        return record;
    }

    public Task CleanHistory(string agentId)
    {
        throw new NotImplementedException();
    }

    public List<RoleDialogModel> GetDialogHistory(int lastCount = 50)
    {
        var dialogs = _storage.GetDialogs(_conversationId);
        return dialogs
            .Where(x => x.CreatedAt > DateTime.UtcNow.AddHours(-24))
            .TakeLast(lastCount)
            .ToList();
    }

    public void SetConversationId(string conversationId, List<string> states)
    {
        _conversationId = conversationId;
        _state.Load(_conversationId);
        states.ForEach(x => _state.SetState(x.Split('=')[0], x.Split('=')[1]));
    }
}
