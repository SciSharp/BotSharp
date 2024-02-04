using BotSharp.Abstraction.Tasks.Models;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Agents;

public class AgentTaskViewModel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string Content { get; set; }
    public bool Enabled { get; set; }
    [JsonPropertyName("created_datetime")]
    public DateTime CreatedDateTime { get; set; }
    [JsonPropertyName("updated_datetime")]
    public DateTime UpdatedDateTime { get; set; }
    [JsonPropertyName("agent_id")]
    public string AgentId { get; set; }
    [JsonPropertyName("agent_name")]
    public string AgentName { get; set; }
    [JsonPropertyName("direct_agent_id")]
    public string? DirectAgentId { get; set; }

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
            CreatedDateTime = task.CreatedDateTime,
            UpdatedDateTime = task.UpdatedDateTime,
            DirectAgentId = task?.DirectAgentId
        };
    }
}
