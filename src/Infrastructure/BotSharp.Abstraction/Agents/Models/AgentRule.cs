using BotSharp.Abstraction.Rules.Enums;

namespace BotSharp.Abstraction.Agents.Models;

public class AgentRule
{
    [JsonPropertyName("trigger_name")]
    public string TriggerName { get; set; } = string.Empty;

    [JsonPropertyName("disabled")]
    public bool Disabled { get; set; }

    [JsonPropertyName("criteria")]
    public string Criteria { get; set; } = string.Empty;

    [JsonPropertyName("delay")]
    public RuleDelay? Delay { get; set; }

    [JsonPropertyName("action")]
    public string? Action { get; set; }
}

public class RuleDelay
{
    public int Quantity { get; set; }
    public string Unit { get; set; }

    public TimeSpan? Parse()
    {
        TimeSpan? ts = null;

        switch (Unit)
        {
            case RuleDelayUnit.Second:
                ts = TimeSpan.FromSeconds(Quantity);
                break;
            case RuleDelayUnit.Minute:
                ts = TimeSpan.FromMinutes(Quantity);
                break;
            case RuleDelayUnit.Hour:
                ts = TimeSpan.FromHours(Quantity);
                break;
            case RuleDelayUnit.Day:
                ts = TimeSpan.FromDays(Quantity);
                break;
        }

        return ts;
    }
}
