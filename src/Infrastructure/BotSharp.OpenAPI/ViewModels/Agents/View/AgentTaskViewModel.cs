using BotSharp.Abstraction.Tasks.Models;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Agents;

public class AgentTaskViewModel
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Content { get; set; } = null!;
    public bool Enabled { get; set; }

    public string Status { get; set; } = null!;

    [JsonPropertyName("created_time")]
    public DateTime CreatedTime { get; set; }

    [JsonPropertyName("updated_time")]
    public DateTime UpdatedTime { get; set; }

    [JsonPropertyName("agent_id")]
    public string AgentId { get; set; } = null!;

    [JsonPropertyName("agent_name")]
    public string AgentName { get; set; } = null!;

    public static AgentTaskViewModel From(AgentTask task)
    {
        return new AgentTaskViewModel
        {
            Id = task.Id,
            Name = task.Name,
            Description = task.Description,
            Content = task.Content,
            Enabled = task.Enabled,
            AgentId = task.AgentId,
            AgentName = task.Agent?.Name,
            Status = task.Status,
            CreatedTime = task.CreatedTime,
            UpdatedTime = task.UpdatedTime
        };
    }
}
