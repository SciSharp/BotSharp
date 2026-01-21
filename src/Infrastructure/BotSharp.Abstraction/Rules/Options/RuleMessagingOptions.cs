namespace BotSharp.Abstraction.Rules.Options;

public class RuleMessagingOptions
{
    /// <summary>
    /// Message payload
    /// </summary>
    public string Payload { get; set; }

    /// <summary>
    /// Exchange
    /// </summary>
    public string Exchange { get; set; }

    /// <summary>
    /// Routing key
    /// </summary>
    public string RoutingKey { get; set; }

    /// <summary>
    /// Delayed message id
    /// </summary>
    public string? MessageId { get; set; }

    /// <summary>
    /// Arguments
    /// </summary>
    public Dictionary<string, object?> Arguments { get; set; } = new();

    public override string ToString()
    {
        return $"{Exchange}-{RoutingKey} => {Payload}";
    }
}
