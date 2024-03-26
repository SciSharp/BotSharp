using BotSharp.Abstraction.Conversations.Models;

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

        if (ContainsState(name) && _states.TryGetValue(name, out var pair))
        {
            preValue = pair?.Values.LastOrDefault()?.Data ?? string.Empty;
        }

        if (!ContainsState(name) || preValue != currentValue)
        {
            _logger.LogInformation($"[STATE] {name} = {value}");
            foreach (var hook in hooks)
            {
                hook.OnStateChanged(name, preValue, currentValue).Wait();
            }

            var routingCtx = _services.GetRequiredService<IRoutingContext>();
            var newPair = new StateKeyValue
            {
                Key = name,
                Versioning = isNeedVersion
            };

            var newValue = new StateValue
            {
                Data = currentValue,
                MessageId = routingCtx.MessageId,
                Active = true,
                UpdateTime = DateTime.UtcNow,
            };

            if (!isNeedVersion || !_states.ContainsKey(name))
            {
                newPair.Values = new List<StateValue> { newValue };
                _states[name] = newPair;
            }
            else
            {
                _states[name].Values.Add(newValue);
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
                var value = state.Value?.Values?.LastOrDefault();
                if (value == null || !value.Active) continue;

                var data = value.Data ?? string.Empty;
                curStates[state.Key] = data;
                _logger.LogInformation($"[STATE] {state.Key} : {data}");
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
            states.Add(dic.Value);
        }

        _db.UpdateConversationStates(_conversationId, states);
        _logger.LogInformation($"Saved states of conversation {_conversationId}");
    }

    public void CleanStates()
    {
        var utcNow = DateTime.UtcNow;
        foreach (var key in _states.Keys)
        {
            var value = _states[key];
            if (value == null || !value.Versioning || value.Values.IsNullOrEmpty()) continue;

            var lastValue = value.Values.LastOrDefault();
            if (lastValue == null || !lastValue.Active) continue;

            value.Values.Add(new StateValue
            {
                Data = lastValue.Data,
                MessageId = lastValue.MessageId,
                Active = false,
                UpdateTime = utcNow
            });
        }
    }

    public Dictionary<string, string> GetStates()
    {
        var curStates = new Dictionary<string, string>();
        foreach (var state in _states)
        {
            var value = state.Value?.Values?.LastOrDefault();
            if (value == null || !value.Active) continue;

            curStates[state.Key] = value.Data ?? string.Empty;
        }
        return curStates;
    }

    public string GetState(string name, string defaultValue = "")
    {
        if (!_states.ContainsKey(name) || _states[name].Values.IsNullOrEmpty() || !_states[name].Values.Last().Active)
        {
            return defaultValue;
        }

        return _states[name].Values.Last().Data;
    }

    public void Dispose()
    {
        Save();
    }

    public bool ContainsState(string name)
    {
        return _states.ContainsKey(name)
            && !_states[name].Values.IsNullOrEmpty()
            && _states[name].Values.LastOrDefault()?.Active == true
            && !string.IsNullOrEmpty(_states[name].Values.Last().Data);
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
