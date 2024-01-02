using BotSharp.Abstraction.Repositories;
using System.Linq;

namespace BotSharp.Core.Conversations.Services;

/// <summary>
/// Maintain the conversation state
/// </summary>
public class ConversationStateService : IConversationStateService, IDisposable
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _services;
    private ConversationState _states;
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
    }

    public string GetConversationId() => _conversationId;


    /// <summary>
    /// Set conversation state
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <param name="isNeedVersion">whether the state is related to message or not</param>
    /// <returns></returns>
    public IConversationStateService SetState<T>(string name, T value, bool isNeedVersion = true)
    {
        if (value == null)
        {
            return this;
        }

        var preValue = string.Empty;
        var currentValue = value.ToString();
        var hooks = _services.GetServices<IConversationHook>();

        if (_states.TryGetValue(name, out var values))
        {
            preValue = values?.LastOrDefault()?.Data ?? string.Empty;
        }

        if (!_states.ContainsKey(name) || preValue != currentValue)
        {
            _logger.LogInformation($"[STATE] {name} = {value}");
            foreach (var hook in hooks)
            {
                hook.OnStateChanged(name, preValue, currentValue).Wait();
            }

            var stateValue = new StateValue
            {
                Data = currentValue,
                UpdateTime = DateTime.UtcNow
            };

            if (!_states.ContainsKey(name) || !isNeedVersion)
            {
                _states[name] = new List<StateValue> { stateValue };
            }
            else
            {
                _states[name].Add(stateValue);
            }
        }

        return this;
    }

    public Dictionary<string, string> Load(string conversationId)
    {
        _conversationId = conversationId;

        _states = _db.GetConversationStates(_conversationId);
        var curStates = new Dictionary<string, string>();

        if (!_states.IsNullOrEmpty())
        {
            foreach (var state in _states)
            {
                var value = state.Value?.LastOrDefault()?.Data ?? string.Empty;
                curStates[state.Key] = value;
                _logger.LogInformation($"[STATE] {state.Key} : {value}");
            }
        }

        _logger.LogInformation($"Loaded conversation states: {_conversationId}");
        var hooks = _services.GetServices<IConversationHook>();
        foreach (var hook in hooks)
        {
            hook.OnStateLoaded(_states).Wait();
        }

        return curStates;
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
        _logger.LogInformation($"Saved states of conversation {_conversationId}");
    }

    public void CleanState()
    {
        _states.Clear();
    }

    public Dictionary<string, string> GetStates()
    {
        var curStates = new Dictionary<string, string>();
        foreach (var state in _states)
        {
            curStates[state.Key] = state.Value?.LastOrDefault()?.Data ?? string.Empty;
        }
        return curStates;
    }

    public string GetState(string name, string defaultValue = "")
    {
        if (!_states.ContainsKey(name) || _states[name].IsNullOrEmpty())
        {
            return defaultValue;
        }

        return _states[name].Last().Data;
    }

    public void Dispose()
    {
        Save();
    }

    public bool ContainsState(string name)
    {
        return _states.ContainsKey(name)
            && !_states[name].IsNullOrEmpty()
            && !string.IsNullOrEmpty(_states[name].Last().Data);
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
}
