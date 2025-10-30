using System.Text.Json;

namespace BotSharp.Abstraction.Rules;

public interface IRuleTrigger
{
    string Channel => throw new NotImplementedException("Please set the channel of trigger");

    string Name => throw new NotImplementedException("Please set the name of trigger");

    string EntityType { get; set; }

    string EntityId { get; set; }

    /// <summary>
    /// The default arguments as input to code trigger (display purpose)
    /// </summary>
    JsonDocument OutputArgs => JsonDocument.Parse("{}");
}
