namespace BotSharp.Core.Rules.Triggers;

public interface IRuleTrigger
{
    string Channel => throw new NotImplementedException("Please set the channel of trigger");

    string EventName { get; set; }
    string EntityType { get; set; }

    string EntityId { get; set; }
}
