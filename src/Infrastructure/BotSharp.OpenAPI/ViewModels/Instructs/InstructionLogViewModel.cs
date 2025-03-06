using BotSharp.Abstraction.Loggers.Models;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Instructs;

public class InstructionLogViewModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    [JsonPropertyName("agent_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AgentId { get; set; }

    [JsonPropertyName("agent_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
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

    [JsonPropertyName("user_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UserName { get; set; }

    [JsonPropertyName("states")]
    public Dictionary<string, string> States { get; set; } = [];

    [JsonPropertyName("created_time")]
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    public static InstructionLogViewModel From(InstructionLogModel log)
    {
        return new InstructionLogViewModel
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
            States = log.States,
            CreatedTime = log.CreatedTime
        };
    }
}
