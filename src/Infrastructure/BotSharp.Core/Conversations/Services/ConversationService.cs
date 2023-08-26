using BotSharp.Abstraction.Conversations.Models;
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
