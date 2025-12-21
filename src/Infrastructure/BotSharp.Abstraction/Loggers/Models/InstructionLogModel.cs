using System.Text.Json;

namespace BotSharp.Abstraction.Loggers.Models;

public class InstructionLogModel : InstructionLogBaseModel
{
    public Dictionary<string, string> States { get; set; } = [];

    public static InstructionLogModel From(InstructionFileLogModel log)
    {
        return new InstructionLogModel
        {
            Id = log.Id,
            AgentId = log.AgentId,
            AgentName = log.AgentName,
            Provider = log.Provider,
            Model = log.Model,
            TemplateName = log.TemplateName,
            UserMessage = log.UserMessage,
            SystemInstruction = log.SystemInstruction,
            CompletionText = log.CompletionText,
            UserId = log.UserId,
            UserName = log.UserName,
            CreatedTime = log.CreatedTime
        };
    }
}

public class InstructionFileLogModel : InstructionLogBaseModel
{
    [JsonPropertyName("states")]
    public Dictionary<string, JsonDocument> States { get; set; } = [];

    public static InstructionFileLogModel From(InstructionLogModel log)
    {
        return new InstructionFileLogModel
        {
            Id = log.Id,
            AgentId = log.AgentId,
            AgentName = log.AgentName,
            Provider = log.Provider,
            Model = log.Model,
            TemplateName = log.TemplateName,
            UserMessage = log.UserMessage,
            SystemInstruction = log.SystemInstruction,
            CompletionText = log.CompletionText,
            UserId = log.UserId,
            UserName = log.UserName,
            CreatedTime = log.CreatedTime
        }; 
    }
}

public class InstructionLogBaseModel
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

    [JsonPropertyName("created_time")]
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
}
