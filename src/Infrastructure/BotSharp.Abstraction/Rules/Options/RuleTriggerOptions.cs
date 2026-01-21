using BotSharp.Abstraction.Infrastructures.MessageQueues.Models;
using System.Text.Json;

namespace BotSharp.Abstraction.Rules.Options;

public class RuleTriggerOptions
{
    public CriteriaOptions? Criteria { get; set; }
    public DelayMessageOptions? DelayMessage { get; set; }
}

public class CriteriaOptions
{
    /// <summary>
    /// Code processor provider
    /// </summary>
    public string? CodeProcessor { get; set; }

    /// <summary>
    /// Code script name
    /// </summary>
    public string? CodeScriptName { get; set; }

    /// <summary>
    /// Argument name as an input key to the code script
    /// </summary>
    public string? ArgumentName { get; set; }

    /// <summary>
    /// Json arguments as an input value to the code script
    /// </summary>
    public JsonDocument? ArgumentContent { get; set; }
}

public class DelayMessageOptions
{
    public string Payload { get; set; }
    public string Exchange { get; set; }
    public string RoutingKey { get; set; }
    public string? MessageId { get; set; }
    public Dictionary<string, object?> Arguments { get; set; } = new();

    public override string ToString()
    {
        return $"{Exchange}-{RoutingKey} => {Payload}";
    }
}