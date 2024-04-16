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
    private readonly IBotSharpRepository _db;
    private string _conversationId;
    /// <summary>
    /// States in the current round of conversation
    /// </summary>
    private ConversationState _curStates;
    /// <summary>
    /// States in the previous rounds of conversation
    /// </summary>
    private ConversationState _historyStates;

    public ConversationStateService(ILogger<ConversationStateService> logger,
        IServiceProvider services,
        IBotSharpRepository db)
    {
        _logger = logger;
        _services = services;
        _db = db;
        _curStates = new ConversationState();
        _historyStates = new ConversationState();
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

        if (ContainsState(name) && _curStates.TryGetValue(name, out var pair))
        {
            var leafNode = pair?.Values?.LastOrDefault();
            preActiveRounds = leafNode?.ActiveRounds;
            preValue = leafNode?.Data ?? string.Empty;
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

        if (!isNeedVersion || !_curStates.ContainsKey(name))
        {
            newPair.Values = new List<StateValue> { newValue };
            _curStates[name] = newPair;
        }
        else
        {
            _curStates[name].Values.Add(newValue);
        }

        return this;
    }

    public Dictionary<string, string> Load(string conversationId, bool isReadOnly = false)
    {
        _conversationId = !isReadOnly ? conversationId : null;

        var routingCtx = _services.GetRequiredService<IRoutingContext>();
        var curMsgId = routingCtx.MessageId;

        _historyStates = _db.GetConversationStates(conversationId);
        var dialogs = _db.GetConversationDialogs(conversationId);
        var userDialogs = dialogs.Where(x => x.MetaData?.Role == AgentRole.User || x.MetaData?.Role == UserRole.Client)
                                 .GroupBy(x => x.MetaData?.MessageId)
                                 .Select(g => g.First())
                                 .OrderBy(x => x.MetaData?.CreateTime)
                                 .ToList();
        var curMsgIndex = userDialogs.FindIndex(x => !string.IsNullOrEmpty(curMsgId) && x.MetaData?.MessageId == curMsgId);
        curMsgIndex = curMsgIndex < 0 ? userDialogs.Count() : curMsgIndex;

        var endNodes = new Dictionary<string, string>();
        if (_historyStates.IsNullOrEmpty()) return endNodes;

        foreach (var state in _historyStates)
        {
            var key = state.Key;
            var value = state.Value;
            var leafNode = value?.Values?.LastOrDefault();
            if (leafNode == null) continue;

            _curStates[key] = new StateKeyValue
            {
                Key = key,
                Versioning = value.Versioning,
                Readonly = value.Readonly,
                Values = new List<StateValue> { leafNode }
            };

            if (!leafNode.Active) continue;

            // Handle state active rounds
            if (leafNode.ActiveRounds > 0)
            {
                var stateMsgIndex = userDialogs.FindIndex(x => !string.IsNullOrEmpty(x.MetaData?.MessageId) && x.MetaData.MessageId == leafNode.MessageId);
                if (stateMsgIndex >= 0 && curMsgIndex - stateMsgIndex >= leafNode.ActiveRounds)
                {
                    _curStates[key].Values.Add(new StateValue
                    {
                        Data = leafNode.Data,
                        MessageId = curMsgId,
                        Active = false,
                        ActiveRounds = leafNode.ActiveRounds,
                        DataType = leafNode.DataType,
                        Source = leafNode.Source,
                        UpdateTime = DateTime.UtcNow
                    });
                    continue;
                }
            }

            var data = leafNode.Data ?? string.Empty;
            endNodes[state.Key] = data;
            _logger.LogInformation($"[STATE] {key} : {data}");
        }

        _logger.LogInformation($"Loaded conversation states: {conversationId}");
        var hooks = _services.GetServices<IConversationHook>();
        foreach (var hook in hooks)
        {
            hook.OnStateLoaded(_curStates).Wait();
        }

        return endNodes;
    }

    public void Save()
    {
        if (_conversationId == null)
        {
            return;
        }

        var states = new List<StateKeyValue>();

        foreach (var pair in _curStates)
        {
            var key = pair.Key;
            var curValue = pair.Value;

            if (!_historyStates.TryGetValue(key, out var historyValue)
                || historyValue == null
                || historyValue.Values.IsNullOrEmpty()
                || !curValue.Versioning)
            {
                states.Add(curValue);
            }
            else
            {
                var historyValues = historyValue.Values.Take(historyValue.Values.Count - 1).ToList();
                var newValues = historyValues.Concat(curValue.Values).ToList();
                var updatedNode = new StateKeyValue
                {
                    Key = pair.Key,
                    Versioning = curValue.Versioning,
                    Readonly = curValue.Readonly,
                    Values = newValues
                };
                states.Add(updatedNode);
            }
        }

        _db.UpdateConversationStates(_conversationId, states);
        _logger.LogInformation($"Saved states of conversation {_conversationId}");
    }

    public bool RemoveState(string name)
    {
        if (!ContainsState(name)) return false;

        var routingCtx = _services.GetRequiredService<IRoutingContext>();
        var value = _curStates[name];
        var leafNode = value?.Values?.LastOrDefault();
        if (value == null || !value.Versioning || leafNode == null) return false;

        _curStates[name].Values.Add(new StateValue
        {
            Data = leafNode.Data,
            MessageId = routingCtx.MessageId,
            Active = false,
            ActiveRounds = leafNode.ActiveRounds,
            DataType = leafNode.DataType,
            Source = leafNode.Source,
            UpdateTime = DateTime.UtcNow
        });

        var hooks = _services.GetServices<IConversationHook>();
        foreach (var hook in hooks)
        {
            hook.OnStateChanged(new StateChangeModel
            {
                ConversationId = _conversationId,
                MessageId = routingCtx.MessageId,
                Name = name,
                BeforeValue = leafNode.Data,
                BeforeActiveRounds = leafNode.ActiveRounds,
                AfterValue = null,
                AfterActiveRounds = leafNode.ActiveRounds,
                DataType = leafNode.DataType,
                Source = leafNode.Source,
                Readonly = value.Readonly
            }).Wait();
        }

        return true;
    }

    public void CleanStates()
    {
        var routingCtx = _services.GetRequiredService<IRoutingContext>();
        var curMsgId = routingCtx.MessageId;
        var utcNow = DateTime.UtcNow;

        foreach (var key in _curStates.Keys)
        {
            var value = _curStates[key];
            if (value == null || !value.Versioning || value.Values.IsNullOrEmpty()) continue;

            var leafNode = value.Values.LastOrDefault();
            if (leafNode == null || !leafNode.Active) continue;

            value.Values.Add(new StateValue
            {
                Data = leafNode.Data,
                MessageId = curMsgId,
                Active = false,
                ActiveRounds = leafNode.ActiveRounds,
                DataType = leafNode.DataType,
                Source = leafNode.Source,
                UpdateTime = utcNow
            });
        }
    }

    public Dictionary<string, string> GetStates()
    {
        var endNodes = new Dictionary<string, string>();
        foreach (var state in _curStates)
        {
            var value = state.Value?.Values?.LastOrDefault();
            if (value == null || !value.Active) continue;

            endNodes[state.Key] = value.Data ?? string.Empty;
        }
        return endNodes;
    }

    public string GetState(string name, string defaultValue = "")
    {
        if (!_curStates.ContainsKey(name) || _curStates[name].Values.IsNullOrEmpty() || !_curStates[name].Values.Last().Active)
        {
            return defaultValue;
        }

        return _curStates[name].Values.Last().Data;
    }

    public void Dispose()
    {
        Save();
    }

    public bool ContainsState(string name)
    {
        return _curStates.ContainsKey(name)
            && !_curStates[name].Values.IsNullOrEmpty()
            && _curStates[name].Values.LastOrDefault()?.Active == true
            && !string.IsNullOrEmpty(_curStates[name].Values.Last().Data);
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
                var propertyValue = property.Value;
                var stateValue = propertyValue.ToString();
                if (!string.IsNullOrEmpty(stateValue))
                {
                    if (propertyValue.ValueKind == JsonValueKind.True ||
                        propertyValue.ValueKind == JsonValueKind.False)
                    {
                        stateValue = stateValue?.ToLower();
                    }

                    SetState(property.Name, stateValue, source: StateSource.Application);
                }
            }
        }
    }
}
