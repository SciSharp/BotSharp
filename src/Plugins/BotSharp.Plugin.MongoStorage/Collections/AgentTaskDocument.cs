using BotSharp.Abstraction.Tasks.Models;

namespace BotSharp.Plugin.MongoStorage.Collections;

public class AgentTaskDocument : MongoBase
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string Content { get; set; } = default!;
    public bool Enabled { get; set; }
    public string AgentId { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTime CreatedTime { get; set; }
    public DateTime UpdatedTime { get; set; }

    public static AgentTask ToDomainModel(AgentTaskDocument model)
    {
        return new AgentTask
        {
            Id = model.Id,
            Description = model.Description,
            Content = model.Content,
            Enabled = model.Enabled,
            AgentId = model.AgentId,
            Status = model.Status,
            CreatedTime = model.CreatedTime,
            UpdatedTime = model.UpdatedTime
        };
    }

    public static AgentTaskDocument ToMongoModel(AgentTask model)
    {
        return new AgentTaskDocument
        {
            Id = model.Id,
            Description = model.Description,
            Content = model.Content,
            Enabled = model.Enabled,
            AgentId = model.AgentId,
            Status = model.Status,
            CreatedTime = model.CreatedTime,
            UpdatedTime = model.UpdatedTime
        };
    }
}
