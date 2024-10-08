using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Plugin.EntityFrameworkCore.Mappers;

public static class StateMappers
{
    public static Entities.State ToEntity(this StateKeyValue state)
    {
        return new Entities.State
        {
            Id = Guid.NewGuid().ToString(),
            Key = state.Key,
            Versioning = state.Versioning,
            Readonly = state.Readonly,
            Values = state.Values?.Select(x => x.ToEntity())?.ToList() ?? new List<Entities.StateValue>()
        };
    }

    public static Entities.State ToEntity(this StateKeyValue state, Entities.ConversationState conversationState)
    {
        var stateId = Guid.NewGuid().ToString();
        return new Entities.State
        {
            Id = stateId,
            Key = state.Key,
            Versioning = state.Versioning,
            Readonly = state.Readonly,
            ConversationStateId = conversationState.Id,
            Values = state.Values?.Select(x => x.ToEntity(stateId))?.ToList() ?? new List<Entities.StateValue>()
        };
    }

    public static StateKeyValue ToModel(this Entities.State state)
    {
        return new StateKeyValue
        {
            Key = state.Key,
            Versioning = state.Versioning,
            Readonly = state.Readonly,
            Values = state.Values?.Select(x => x.ToModel())?.ToList() ?? new List<StateValue>()
        };
    }

    public static Entities.StateValue ToEntity(this StateValue element)
    {
        return new Entities.StateValue
        {
            Id = Guid.NewGuid().ToString(),
            Data = element.Data,
            MessageId = element.MessageId,
            Active = element.Active,
            ActiveRounds = element.ActiveRounds,
            DataType = element.DataType,
            Source = element.Source,
            UpdateTime = element.UpdateTime
        };
    }


    public static Entities.StateValue ToEntity(this StateValue element, string stateId)
    {
        return new Entities.StateValue
        {
            Id = Guid.NewGuid().ToString(),
            StateId = stateId,
            Data = element.Data,
            MessageId = element.MessageId,
            Active = element.Active,
            ActiveRounds = element.ActiveRounds,
            DataType = element.DataType,
            Source = element.Source,
            UpdateTime = element.UpdateTime
        };
    }

    public static StateValue ToModel(this Entities.StateValue element)
    {
        return new StateValue
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
}
