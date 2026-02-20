using System.Text.Json.Serialization;

namespace BotSharp.Core.Rules.Models;

public class RuleTriggerActionRequest
{
    [JsonPropertyName("trigger_name")]
    public string TriggerName { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("states")]
    public IEnumerable<MessageState>? States { get; set; }

    [JsonPropertyName("options")]
    public RuleTriggerOptions? Options { get; set; }
}
