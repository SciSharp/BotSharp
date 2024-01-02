using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

public class StateMongoElement
{
    public string Key { get; set; }
    public List<StateValueMongoElement> Values { get; set; }

    public static StateMongoElement ToMongoElement(StateKeyValue state)
    {
        return new StateMongoElement
        {
            Key = state.Key,
            Values = state.Values?.Select(x => StateValueMongoElement.ToMongoElement(x))?.ToList() ?? new List<StateValueMongoElement>()
        };
    }

    public static StateKeyValue ToDomainElement(StateMongoElement state)
    {
        return new StateKeyValue
        {
            Key = state.Key,
            Values = state.Values?.Select(x => StateValueMongoElement.ToDomainElement(x))?.ToList() ?? new List<StateValue>()
        };
    }
}

public class StateValueMongoElement
{
    public string Data { get; set; }
    public DateTime UpdateTime { get; set; }

    public static StateValueMongoElement ToMongoElement(StateValue element)
    {
        return new StateValueMongoElement
        {
            Data = element.Data,
            UpdateTime = element.UpdateTime
        };
    }

    public static StateValue ToDomainElement(StateValueMongoElement element)
    {
        return new StateValue
        {
            Data = element.Data,
            UpdateTime = element.UpdateTime
        };
    }
}