namespace BotSharp.Abstraction.Chart.Models;

public class ChartCodeOptions
{
    public string? AgentId { get; set; }
    public string? TemplateName { get; set; }
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Conversation state that can be used to fetch chart data
    /// </summary>
    public string? TargetStateName { get; set; }

    public ChartLlmOptions? Llm { get; set; }
    public List<KeyValue<object>>? States { get; set; }

}

public class ChartLlmOptions
{
    public string? Provider { get; set; }
    public string? Model { get; set; }
    public int? MaxOutputTokens { get; set; }
    public string? ReasoningEffortLevel { get; set; }
}