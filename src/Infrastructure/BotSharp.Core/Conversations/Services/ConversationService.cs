using BotSharp.Abstraction.Conversations.Enums;
using BotSharp.Abstraction.Hooks;
using BotSharp.Abstraction.Models;

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
    private const string AIAssistant = BuiltInAgentId.AIAssistant;

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

    public async Task<bool> DeleteConversations(IEnumerable<string> ids)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var fileStorage = _services.GetRequiredService<IFileStorageService>();
        var isDeleted = db.DeleteConversations(ids);
        fileStorage.DeleteConversationFiles(ids);
        return await Task.FromResult(isDeleted);
    }

    public async Task<Conversation> UpdateConversationTitle(string id, string title)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        db.UpdateConversationTitle(id, title);
        var conversation = db.GetConversation(id);
        return conversation;
    }

    public async Task<Conversation> UpdateConversationTitleAlias(string id, string titleAlias)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        db.UpdateConversationTitleAlias(id, titleAlias);
        var conversation = db.GetConversation(id);
        return conversation;
    }

    public async Task<bool> UpdateConversationTags(string conversationId, List<string> toAddTags, List<string> toDeleteTags)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        return db.UpdateConversationTags(conversationId, toAddTags, toDeleteTags);
    }

    public async Task<bool> UpdateConversationMessage(string conversationId, UpdateMessageRequest request)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        return db.UpdateConversationMessage(conversationId, request);
    }

    public async Task<Conversation> GetConversation(string id, bool isLoadStates = false)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var conversation = db.GetConversation(id);
        return conversation;
    }

    public async Task<PagedItems<Conversation>> GetConversations(ConversationFilter filter)
    {
        if (filter == null)
        {
            filter = ConversationFilter.Empty();
        }

        var db = _services.GetRequiredService<IBotSharpRepository>();
        var conversations = db.GetConversations(filter);
        return conversations;
    }

    public async Task<List<Conversation>> GetLastConversations()
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        return db.GetLastConversations();
    }

    public async Task<List<string>> GetIdleConversations(int batchSize, int messageLimit, int bufferHours, IEnumerable<string> excludeAgentIds)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        return db.GetIdleConversations(batchSize, messageLimit, bufferHours, excludeAgentIds ?? new List<string>());
    }

    public async Task<Conversation> NewConversation(Conversation sess)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var user = db.GetUserById(_user.Id);
        var foundUserId = user?.Id ?? string.Empty;

        var record = sess;
        record.Id = sess.Id.IfNullOrEmptyAs(Guid.NewGuid().ToString());
        record.UserId = sess.UserId.IfNullOrEmptyAs(foundUserId);
        record.Tags = sess.Tags;
        record.Title = string.IsNullOrEmpty(record.Title) ? "New Conversation" : record.Title;

        db.CreateNewConversation(record);

        var hooks = _services.GetHooks<IConversationHook>(record.AgentId);

        foreach (var hook in hooks)
        {
            // If user connect agent first time
            await hook.OnUserAgentConnectedInitially(sess);

            await hook.OnConversationInitialized(record);
        }

        return record;
    }

    public Task CleanHistory(string agentId)
    {
        throw new NotImplementedException();
    }

    public List<RoleDialogModel> GetDialogHistory(int lastCount = 100, bool fromBreakpoint = true, IEnumerable<string>? includeMessageTypes = null)
    {
        if (string.IsNullOrEmpty(_conversationId))
        {
            throw new ArgumentNullException("ConversationId is null.");
        }

        var dialogs = _storage.GetDialogs(_conversationId);

        if (!includeMessageTypes.IsNullOrEmpty())
        {
            dialogs = dialogs.Where(x => string.IsNullOrEmpty(x.MessageType) || includeMessageTypes.Contains(x.MessageType)).ToList();
        }
        else
        {
            dialogs = dialogs.Where(x => string.IsNullOrEmpty(x.MessageType) || x.MessageType.IsEqualTo(MessageTypeName.Plain)).ToList();
        }

        if (fromBreakpoint)
        {
            var db = _services.GetRequiredService<IBotSharpRepository>();
            var breakpoint = db.GetConversationBreakpoint(_conversationId);

            if (breakpoint != null)
            {
                dialogs = dialogs.Where(x => x.CreatedAt >= breakpoint.Breakpoint).ToList();
                if (!string.IsNullOrEmpty(breakpoint.Reason))
                {
                    dialogs.Insert(0, new RoleDialogModel(AgentRole.User, breakpoint.Reason));
                }
            }
        }

        var agentMsgCount = GetAgentMessageCount();
        var count = agentMsgCount.HasValue && agentMsgCount.Value > 0 ? agentMsgCount.Value : lastCount;

        return dialogs.TakeLast(count).ToList();
    }

    public void SetConversationId(string conversationId, List<MessageState> states, bool isReadOnly = false)
    {
        _conversationId = conversationId;
        _state.Load(_conversationId, isReadOnly);
        states.ForEach(x => _state.SetState(x.Key, x.Value, activeRounds: x.ActiveRounds, isNeedVersion: !x.Global, source: StateSource.External));
    }

    public async Task<Conversation> GetConversationRecordOrCreateNew(string agentId)
    {
        var converation = await GetConversation(_conversationId);

        // Create conversation if this conversation does not exist
        if (converation == null)
        {
            var state = _services.GetRequiredService<IConversationStateService>();
            var channel = state.GetState("channel");
            var channelId = state.GetState("channel_id");
            var userId = state.GetState("current_user_id");
            var sess = new Conversation
            {
                Id = _conversationId,
                Channel = channel,
                ChannelId = channelId,
                AgentId = agentId,
                UserId = userId,
            };
            converation = await NewConversation(sess);
        }
        
        return converation;
    }

    public bool IsConversationMode()
    {
        return !string.IsNullOrWhiteSpace(_conversationId);
    }


    private int? GetAgentMessageCount()
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var routingCtx = _services.GetRequiredService<IRoutingContext>();

        if (string.IsNullOrEmpty(routingCtx.EntryAgentId)) return null;

        var agent = db.GetAgent(routingCtx.EntryAgentId, basicsOnly: true);
        return agent?.MaxMessageCount;
    }

    public void SaveStates()
    {
        _state.Save();
    }

    public async Task<List<string>> GetConversationStateSearhKeys(ConversationStateKeysFilter filter)
    {
        if (filter == null)
        {
            filter = ConversationStateKeysFilter.Empty();
        }

        var keys = new List<string>();
        if (!filter.PreLoad && string.IsNullOrWhiteSpace(filter.Query))
        {
            return keys;
        }

        var userService = _services.GetRequiredService<IUserService>();
        var db = _services.GetRequiredService<IBotSharpRepository>();

        var (isAdmin, user) = await userService.IsAdminUser(_user.Id);
        filter.UserIds = !isAdmin && user?.Id != null ? [user.Id] : [];
        keys = db.GetConversationStateSearchKeys(filter);
        keys = filter.PreLoad ? keys : keys.Where(x => x.Contains(filter.Query ?? string.Empty, StringComparison.OrdinalIgnoreCase)).ToList();
        return keys.OrderBy(x => x).Take(filter.KeyLimit).ToList();
    }
}
