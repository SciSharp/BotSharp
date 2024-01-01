using BotSharp.Abstraction.Repositories;

namespace BotSharp.Core.Conversations.Services;

/// <summary>
/// Maintain the conversation state
/// </summary>
public class ConversationStateService : IConversationStateService, IDisposable
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _services;
    private ConversationState _states;
    private ConversationHistoryState _historyStates;
    private string _conversationId;
    private readonly IBotSharpRepository _db;

    public ConversationStateService(ILogger<ConversationStateService> logger,
        IServiceProvider services,
        IBotSharpRepository db)
    {
        _logger = logger;
        _services = services;
        _db = db;
        _states = new ConversationState();
        _historyStates = new ConversationHistoryState();
    }

    public string GetConversationId() => _conversationId;


    /// <summary>
    /// Set conversation state
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <param name="isConst">whether the state is related to message or not</param>
    /// <returns></returns>
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

            var historyStateValue = new HistoryStateValue
            {
                MessageId = GetCurrentMessageId(),
                Data = currentValue,
                UpdateTime = DateTime.UtcNow
            };

            if (!_historyStates.ContainsKey(name))
            {
                _historyStates[name] = new List<HistoryStateValue>();
            }

            _historyStates[name].Add(historyStateValue);
        }

        return this;
    }

    public ConversationState Load(string conversationId)
    {
        _conversationId = conversationId;

        var savedStates = _db.GetConversationStates(_conversationId).ToList();
        _historyStates = new ConversationHistoryState(savedStates);

        if (!savedStates.IsNullOrEmpty())
        {
            foreach (var state in savedStates)
            {
                var value = state.Values.LastOrDefault()?.Data ?? string.Empty;
                _states[state.Key] = value;
                _logger.LogInformation($"[STATE] {state.Key} : {value}");
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

        var historyStates = new List<HistoryStateKeyValue>();

        foreach (var dic in _historyStates)
        {
            historyStates.Add(new HistoryStateKeyValue(dic.Key, dic.Value));
        }

        _db.UpdateConversationStates(_conversationId, historyStates);
        _logger.LogInformation($"Saved states of conversation {_conversationId}");
    }

    public void CleanState()
    {
        
    }

    public ConversationState GetStates() => _states;

    public string GetState(string name, string defaultValue = "")
    {
        if (!_states.ContainsKey(name))
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

    public void SaveStateByArgs(JsonDocument args)
    {
        if (args == null)
        {
            return;
        }

        if (args.RootElement is JsonElement root)
        {
            foreach (JsonProperty property in root.EnumerateObject())
            {
                if (!string.IsNullOrEmpty(property.Value.ToString()))
                {
                    SetState(property.Name, property.Value);
                }
            }
        }
    }

    private string? GetCurrentMessageId()
    {
        if (string.IsNullOrEmpty(_conversationId)) return null;

        var dialogs = _db.GetConversationDialogs(_conversationId);
        return dialogs.LastOrDefault()?.MetaData?.MessageId;
    }
}
