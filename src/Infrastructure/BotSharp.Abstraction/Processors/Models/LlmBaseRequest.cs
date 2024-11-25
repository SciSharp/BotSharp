namespace BotSharp.Abstraction.Processors.Models;

public class LlmBaseRequest
{
    [JsonPropertyName("provider")]
    public string Provider { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; }

    [JsonPropertyName("agent_id")]
    public string? AgentId { get; set; }

    [JsonPropertyName("template_name")]
    public string? TemplateName { get; set; }
}
