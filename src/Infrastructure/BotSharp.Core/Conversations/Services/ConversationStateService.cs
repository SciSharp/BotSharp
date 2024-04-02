using BotSharp.Abstraction.Conversations.Enums;
using BotSharp.Abstraction.Users.Enums;

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
    public IConversationStateService SetState<T>(string name, T value, bool isNeedVersion = true,
        int activeRounds = -1, string valueType = StateDataType.String, string source = StateSource.User, bool readOnly = false)
    {
        if (value == null)
        {
            return this;
        }

        var preValue = string.Empty;
        var currentValue = value.ToString();
        var hooks = _services.GetServices<IConversationHook>();
        var curActiveRounds = activeRounds > 0 ? activeRounds : -1;
        int? preActiveRounds = null;

        if (ContainsState(name) && _states.TryGetValue(name, out var pair))
        {
            var lastNode = pair?.Values?.LastOrDefault();
            preActiveRounds = lastNode?.ActiveRounds;
            preValue = lastNode?.Data ?? string.Empty;
        }

        _logger.LogInformation($"[STATE] {name} = {value}");
        var routingCtx = _services.GetRequiredService<IRoutingContext>();

        if (!ContainsState(name) || preValue != currentValue || preActiveRounds != curActiveRounds)
        {
            foreach (var hook in hooks)
            {
                hook.OnStateChanged(new StateChangeModel
                {
                    ConversationId = _conversationId,
                    MessageId = routingCtx.MessageId,
                    Name = name,
                    BeforeValue = preValue,
                    BeforeActiveRounds = preActiveRounds,
                    AfterValue = currentValue,
                    AfterActiveRounds = curActiveRounds,
                    DataType = valueType,
                    Source = source,
                    Readonly = readOnly
                }).Wait();
            }
        }

        var newPair = new StateKeyValue
        {
            Key = name,
            Versioning = isNeedVersion,
            Readonly = readOnly
        };

        var newValue = new StateValue
        {
            Data = currentValue,
            MessageId = routingCtx.MessageId,
            Active = true,
            ActiveRounds = curActiveRounds,
            DataType = valueType,
            Source = source,
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

        return this;
    }

    public Dictionary<string, string> Load(string conversationId)
    {
        _conversationId = conversationId;

        var routingCtx = _services.GetRequiredService<IRoutingContext>();
        var curMsgId = routingCtx.MessageId;
        _states = _db.GetConversationStates(_conversationId);
        var dialogs = _db.GetConversationDialogs(_conversationId);
        var userDialogs = dialogs.Where(x => x.MetaData?.Role == AgentRole.User || x.MetaData?.Role == UserRole.Client)
                                 .OrderBy(x => x.MetaData?.CreateTime)
                                 .ToList();

        var curMsgIndex = userDialogs.FindIndex(x => !string.IsNullOrEmpty(curMsgId) && x.MetaData?.MessageId == curMsgId);
        curMsgIndex = curMsgIndex < 0 ? userDialogs.Count() : curMsgIndex;
        var curStates = new Dictionary<string, string>();

        if (!_states.IsNullOrEmpty())
        {
            foreach (var state in _states)
            {
                var value = state.Value?.Values?.LastOrDefault();
                if (value == null || !value.Active) continue;

                if (value.ActiveRounds > 0)
                {
                    var stateMsgIndex = userDialogs.FindIndex(x => !string.IsNullOrEmpty(x.MetaData?.MessageId) && x.MetaData.MessageId == value.MessageId);
                    if (stateMsgIndex >= 0 && curMsgIndex - stateMsgIndex >= value.ActiveRounds)
                    {
                        state.Value.Values.Add(new StateValue
                        {
                            Data = value.Data,
                            MessageId = curMsgId,
                            Active = false,
                            ActiveRounds = value.ActiveRounds,
                            DataType = value.DataType,
                            Source = value.Source,
                            UpdateTime = DateTime.UtcNow
                        });
                        continue;
                    }
                }

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
        var routingCtx = _services.GetRequiredService<IRoutingContext>();
        var curMsgId = routingCtx.MessageId;
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
                MessageId = curMsgId,
                Active = false,
                ActiveRounds = lastValue.ActiveRounds,
                DataType = lastValue.DataType,
                Source = lastValue.Source,
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
                    SetState(property.Name, property.Value, source: StateSource.Application);
                }
            }
        }
    }
}
