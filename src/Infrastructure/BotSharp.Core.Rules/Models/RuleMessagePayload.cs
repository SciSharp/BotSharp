namespace BotSharp.Core.Rules.Models;

public class RuleMessagePayload
{
    public string AgentId { get; set; }
    public string TriggerName { get; set; }
    public string Channel { get; set; }
    public string Text { get; set; }
    public Dictionary<string, object?> States { get; set; }
    public DateTime Timestamp { get; set; }
}
