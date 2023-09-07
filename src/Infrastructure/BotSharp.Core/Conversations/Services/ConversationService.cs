using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Repositories.Records;

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
        var user = db.Users.FirstOrDefault(x => x.ExternalId == _user.Id);
        var conversations = db.GetConversations(user?.Id);
        return conversations.OrderByDescending(x => x.CreatedTime).ToList();
    }

    public async Task<Conversation> NewConversation(Conversation sess)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var dbSettings = _services.GetRequiredService<BotSharpDatabaseSettings>();
        var conversationSettings = _services.GetRequiredService<ConversationSetting>();
        var user = db.Users.FirstOrDefault(x => x.ExternalId == _user.Id);
        var foundUserId = user?.Id ?? _user.Id;

        var record = sess;
        record.Id = sess.Id.IfNullOrEmptyAs(Guid.NewGuid().ToString());
        record.UserId = sess.UserId.IfNullOrEmptyAs(foundUserId);
        record.Title = "New Conversation";

        db.CreateNewConversation(record);
        return record;
    }

    public Task CleanHistory(string agentId)
    {
        throw new NotImplementedException();
    }

    public List<RoleDialogModel> GetDialogHistory(int lastCount = 20)
    {
        var dialogs = _storage.GetDialogs(_conversationId);
        return dialogs
            .Where(x => x.CreatedAt > DateTime.UtcNow.AddHours(-8))
            .TakeLast(lastCount)
            .ToList();
    }

    public void SetConversationId(string conversationId, string channel)
    {
        _conversationId = conversationId;
        _state.Load(_conversationId);
        _state.SetState("channel", channel);
    }
}
