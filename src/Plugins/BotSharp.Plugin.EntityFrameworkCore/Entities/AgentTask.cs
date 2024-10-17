using System;

namespace BotSharp.Plugin.EntityFrameworkCore.Entities;

public class AgentTask
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string Content { get; set; }
    public bool Enabled { get; set; }
    public string AgentId { get; set; }
    public string? DirectAgentId { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime UpdatedTime { get; set; }
}
