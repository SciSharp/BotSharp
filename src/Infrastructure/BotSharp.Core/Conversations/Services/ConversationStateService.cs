using BotSharp.Abstraction.Repositories;
using System.IO;

namespace BotSharp.Core.Conversations.Services;

/// <summary>
/// Maintain the conversation state
/// </summary>
public class ConversationStateService : IConversationStateService, IDisposable
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _services;
    private ConversationState _states;
    private BotSharpDatabaseSettings _dbSettings;
    private string _conversationId;
    private string _file;
    private readonly IBotSharpRepository _db;
    private List<StateKeyValue> _savedStates;

    public ConversationStateService(ILogger<ConversationStateService> logger,
        IServiceProvider services, 
        BotSharpDatabaseSettings dbSettings,
        IBotSharpRepository db)
    {
        _logger = logger;
        _services = services;
        _dbSettings = dbSettings;
        _db = db;
        _states = new ConversationState();
    }

    public void SetState(string name, string value)
    {
        var hooks = _services.GetServices<IConversationHook>();
        string preValue = _states.ContainsKey(name) ? _states[name] : "";
        if (!_states.ContainsKey(name) || _states[name] != value)
        {
            var currentValue = value;
            _states[name] = currentValue;
            _logger.LogInformation($"Set state: {name} = {value}");
            foreach (var hook in hooks)
            {
                hook.OnStateChanged(name, preValue, currentValue).Wait();
            }
        }
    }

    public ConversationState Load(string conversationId)
    {
        _conversationId = conversationId;

        _savedStates = _db.GetConversationStates(_conversationId);

        if (!_savedStates.IsNullOrEmpty())
        {
            foreach (var data in _savedStates)
            {
                _states[data.Key] = data.Value;
                _logger.LogInformation($"Loaded state: {data.Key}={data.Value}");
            }
        }

        _logger.LogInformation($"Loaded conversation states: {_conversationId}");
        var hooks = _services.GetServices<IConversationHook>();
        foreach (var hook in hooks)
        {
            hook.OnStateLoaded(_states).Wait();
        }

        return _states;
    }

    public void Save()
    {
        if (_conversationId == null)
        {
            return;
        }

        var states = new List<StateKeyValue>();

        foreach (var dic in _states)
        {
            states.Add(new StateKeyValue(dic.Key, dic.Value));
        }

        _db.UpdateConversationStates(_conversationId, states);
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

    public ConversationState GetStates()
        => _states;

    public string GetState(string name, string defaultValue = "")
    {
        if (!_states.ContainsKey(name))
        {
            _states[name] = defaultValue ?? "";
        }
        return _states[name];
    }

    public void Dispose()
    {
        Save();
    }
}
