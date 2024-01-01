using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

public class StateMongoElement
{
    public string Key { get; set; }
    public List<StateValueMongoElement> Values { get; set; }

    public static StateMongoElement ToMongoElement(HistoryStateKeyValue state)
    {
        return new StateMongoElement
        {
            Key = state.Key,
            Values = state.Values?.Select(x => StateValueMongoElement.ToMongoElement(x))?.ToList() ?? new List<StateValueMongoElement>()
        };
    }

    public static HistoryStateKeyValue ToDomainElement(StateMongoElement state)
    {
        return new HistoryStateKeyValue
        {
            Key = state.Key,
            Values = state.Values?.Select(x => StateValueMongoElement.ToDomainElement(x))?.ToList() ?? new List<HistoryStateValue>()
        };
    }
}

public class StateValueMongoElement
{
    public string Data { get; set; }
    public DateTime UpdateTime { get; set; }

    public static StateValueMongoElement ToMongoElement(HistoryStateValue element)
    {
        return new StateValueMongoElement
        {
            Data = element.Data,
            UpdateTime = element.UpdateTime
        };
    }

    public static HistoryStateValue ToDomainElement(StateValueMongoElement element)
    {
        return new HistoryStateValue
        {
            Data = element.Data,
            UpdateTime = element.UpdateTime
        };
    }
}