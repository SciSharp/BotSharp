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

    public IConversationStateService SetState<T>(string name, T value)
    {
        if (value == null)
        {
            return this;
        }

        var currentValue = value.ToString();
        var hooks = _services.GetServices<IConversationHook>();
        string preValue = _states.ContainsKey(name) ? _states[name] : "";
        if (!_states.ContainsKey(name) || _states[name] != currentValue)
        {
            _states[name] = currentValue;
            _logger.LogInformation($"[STATE] {name} = {value}");
            foreach (var hook in hooks)
            {
                hook.OnStateChanged(name, preValue, currentValue).Wait();
            }
        }

        return this;
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
                _logger.LogInformation($"[STATE] {data.Key} : {data.Value}");
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

    public ConversationState GetStates()
        => _states;

    public string GetState(string name, string defaultValue = "")
    {
        if (!_states.ContainsKey(name))
        {
            _states[name] = defaultValue ?? "";
        }

        if (string.IsNullOrEmpty(_states[name]))
        {
            return defaultValue;
        }

        return _states[name];
    }

    public void Dispose()
    {
        Save();
    }

    public bool ContainsState(string name)
    {
        return _states.ContainsKey(name) && !string.IsNullOrEmpty(_states[name]);
    }
}
