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
    /// Programming language
    /// </summary>
    [JsonPropertyName("programming_language")]
    public string? ProgrammingLanguage { get; set; }

    /// <summary>
    /// Data that can be used to fill in the prompt
    /// </summary>
    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Data { get; set; }
}