namespace BotSharp.Abstraction.Coding.Options;

public class CodeGenerationOptions : LlmConfigBase
{
    /// <summary>
    /// Agent id to get instruction
    /// </summary>
    [JsonPropertyName("agent_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AgentId { get; set; }

    /// <summary>
    /// Template (prompt) name
    /// </summary>
    [JsonPropertyName("template_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TemplateName { get; set; }

    /// <summary>
    /// The programming language
    /// </summary>
    [JsonPropertyName("language")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Language { get; set; } = "python";

    /// <summary>
    /// Data that can be used to fill in the prompt
    /// </summary>
    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Data { get; set; }
}