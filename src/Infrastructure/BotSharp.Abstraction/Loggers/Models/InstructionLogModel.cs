using System.Text.Json;

namespace BotSharp.Abstraction.Loggers.Models;

public class InstructionLogModel
{
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Id { get; set; } = default!;

    [JsonPropertyName("agent_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AgentId { get; set; }

    [JsonPropertyName("agent_name")]
    [JsonIgnore]
    public string? AgentName { get; set; }

    [JsonPropertyName("provider")]
    public string Provider { get; set; } = default!;

    [JsonPropertyName("model")]
    public string Model { get; set; } = default!;

    [JsonPropertyName("template_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TemplateName { get; set; }

    [JsonPropertyName("user_message")]
    public string UserMessage { get; set; } = string.Empty;

    [JsonPropertyName("system_instruction")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SystemInstruction { get; set; }

    [JsonPropertyName("completion_text")]
    public string CompletionText { get; set; } = string.Empty;

    [JsonPropertyName("user_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UserId { get; set; }

    [JsonIgnore]
    public string? UserName { get; set; }

    [JsonIgnore]
    public Dictionary<string, string> States { get; set; } = [];

    [JsonPropertyName("states")]
    public Dictionary<string, JsonDocument> InnerStates { get; set; } = [];

    [JsonPropertyName("created_time")]
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
}
