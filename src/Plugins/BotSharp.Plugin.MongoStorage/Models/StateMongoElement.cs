using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

public class StateMongoElement
{
    public string Key { get; set; }
    public bool Versioning { get; set; }
    public List<StateValueMongoElement> Values { get; set; }

    public static StateMongoElement ToMongoElement(StateKeyValue state)
    {
        return new StateMongoElement
        {
            Key = state.Key,
            Versioning = state.Versioning,
            Values = state.Values?.Select(x => StateValueMongoElement.ToMongoElement(x))?.ToList() ?? new List<StateValueMongoElement>()
        };
    }

    public static StateKeyValue ToDomainElement(StateMongoElement state)
    {
        return new StateKeyValue
        {
            Key = state.Key,
            Versioning = state.Versioning,
            Values = state.Values?.Select(x => StateValueMongoElement.ToDomainElement(x))?.ToList() ?? new List<StateValue>()
        };
    }
}

public class StateValueMongoElement
{
    public string Data { get; set; }
    public string? MessageId { get; set; }
    public bool Active { get; set; }
    public int ActiveRounds { get; set; }
    public string DataType { get; set; }
    public string Source { get; set; }

    public DateTime UpdateTime { get; set; }

    public static StateValueMongoElement ToMongoElement(StateValue element)
    {
        return new StateValueMongoElement
        {
            Data = element.Data,
            MessageId = element.MessageId,
            Active = element.Active,
            ActiveRounds = element.ActiveRounds,
            DataType = element.DataType,
            Source = element.Source,
            UpdateTime = element.UpdateTime
        };
    }

    public static StateValue ToDomainElement(StateValueMongoElement element)
    {
        return new StateValue
        {
            Data = element.Data,
            MessageId = element.MessageId,
            Active = element.Active,
            ActiveRounds = element.ActiveRounds,
            DataType= element.DataType,
            Source = element.Source,
            UpdateTime = element.UpdateTime
        };
    }
}