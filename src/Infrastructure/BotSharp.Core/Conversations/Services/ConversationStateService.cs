using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Repositories.Records;
using MongoDB.Bson;
using System.IO;

namespace BotSharp.Core.Conversations.Services;

/// <summary>
/// Maintain the conversation state
/// </summary>
public class ConversationStateService : IConversationStateService, IDisposable
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _services;
    private readonly AgentSettings _agentSettings;
    private readonly IUserIdentity _user;
    private readonly IBotSharpRepository _db;
    private ConversationState _state;
    private MyDatabaseSettings _dbSettings;
    private string _conversationId;
    private string _file;

    public ConversationStateService(ILogger<ConversationStateService> logger,
        IServiceProvider services, 
        MyDatabaseSettings dbSettings,
        AgentSettings agentSettings,
        IUserIdentity user,
        IBotSharpRepository db)
    {
        _logger = logger;
        _services = services;
        _dbSettings = dbSettings;
        _agentSettings = agentSettings;
        _user = user;
        _db = db;
    }

    public void SetState(string name, string value)
    {
        var hooks = _services.GetServices<IConversationHook>();
        string preValue = _state.ContainsKey(name) ? _state[name] : "";
        if (!_state.ContainsKey(name) || _state[name] != value)
        {
            var currentValue = value;
            _state[name] = currentValue;
            _logger.LogInformation($"Set state: {name} = {value}");
            foreach (var hook in hooks)
            {
                hook.OnStateChanged(name, preValue, currentValue).Wait();
            }
        }
    }

    public void SetConversation(string conversationId)
    {
        _conversationId = conversationId;
    }

    public ConversationState Load()
    {
        if (_state != null)
        {
            return _state;
        }

        _state = new ConversationState();

        _file = GetConversationState(_conversationId);

        if (_file != null)
        {
            //var dict = File.ReadAllLines(_file);
            var dict = _file.SplitByNewLine();
            foreach (var line in dict)
            {
                _state[line.Split('=')[0]] = line.Split('=')[1];
            }
        }

        _logger.LogInformation($"Loaded state {_conversationId}");
        var hooks = _services.GetServices<IConversationHook>();
        foreach (var hook in hooks)
        {
            hook.OnStateLoaded(_state).Wait();
        }

        return _state;
    }

    public void Save()
    {
        var states = new StringBuilder();
        var conversation = _db.Conversation.FirstOrDefault(x => x.Id == _conversationId);

        foreach (var dic in _state)
        {
            //states.Add($"{dic.Key}={dic.Value}");
            states.AppendLine($"{dic.Key}={dic.Value}");
        }
        //File.WriteAllLines(_file, states);
        _logger.LogInformation($"Saved state {_conversationId}");

        if (conversation != null)
        {
            conversation.State = states.ToString();
            _db.Transaction<IBotSharpTable>(delegate
            {
                _db.Add<IBotSharpTable>(conversation);
            });
        }
    }

    public void CleanState()
    {
        //File.Delete(_file);
        var conversation = _db.Conversation.FirstOrDefault(x => x.Id == _conversationId);
        if (conversation != null)
        {
            conversation.State = string.Empty;
            _db.Transaction<IBotSharpTable>(delegate
            {
                _db.Add<IBotSharpTable>(conversation);
            });
        }
    }

    private string GetStorageFile(string conversationId)
    {
        var dir = Path.Combine(_dbSettings.FileRepository, "conversations", conversationId);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return Path.Combine(dir, "state.dict");
    }

    private string GetConversationState(string conversationId)
    {
        var conversation = _db.Conversation.FirstOrDefault(x => x.Id == conversationId);
        if (conversation == null)
        {
            var user = _db.User.FirstOrDefault(x => x.ExternalId == _user.Id);
            var record = new ConversationRecord()
            {
                Id = ObjectId.GenerateNewId().ToString(),
                AgentId = _agentSettings.RouterId,
                UserId = user?.Id ?? ObjectId.GenerateNewId().ToString(),
                Title = "New Conversation",
                Dialog = string.Empty,
                State = string.Empty
            };

            _db.Transaction<IBotSharpTable>(delegate
            {
                _db.Add<IBotSharpTable>(record);
            });

            conversation = _db.Conversation.FirstOrDefault(x => x.Id == record.Id);
        }

        return conversation.State ?? string.Empty;
    }

    public string GetState(string name)
    {
        if (!_state.ContainsKey(name))
        {
            _state[name] = "";
        }
        return _state[name];
    }

    public void Dispose()
    {
        Save();
    }
}
