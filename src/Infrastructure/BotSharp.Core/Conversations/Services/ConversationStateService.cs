using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Repositories.Models;
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
    private BotSharpDatabaseSettings _dbSettings;
    private string _conversationId;
    private string _file;
    private List<KeyValueModel> _savedStates;

    public ConversationStateService(ILogger<ConversationStateService> logger,
        IServiceProvider services, 
        BotSharpDatabaseSettings dbSettings,
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
        _savedStates = _db.GetConversationState(_conversationId);

        if (_savedStates != null)
        {
            foreach (var data in _savedStates)
            {
                _state[data.Key] = data.Value;
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
        var states = new List<KeyValueModel>();

        foreach (var dic in _state)
        {
            states.Add(new KeyValueModel(dic.Key, dic.Value));
        }

        _db.UpdateConversationState(_conversationId, states);
        _logger.LogInformation($"Saved state {_conversationId}");
    }

    public void CleanState()
    {
        //File.Delete(_file);
    }

    private string GetStorageFile(string conversationId)
    {
        var dir = Path.Combine(_dbSettings.FileRepository, "conversations", conversationId);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var stateFile = Path.Combine(dir, "state.dict");
        if (!File.Exists(stateFile))
        {
            File.WriteAllText(stateFile, "");
        }
        return stateFile;
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
