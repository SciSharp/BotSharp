using System;

namespace BotSharp.Plugin.EntityFrameworkCore.Entities;

public class Conversation
{
    public string Id { get; set; }
    public string AgentId { get; set; }
    public string UserId { get; set; }
    public string? TaskId { get; set; }
    public string Title { get; set; }
    public string TitleAlias { get; set; }
    public string Channel { get; set; }
    public string Status { get; set; }
    public int DialogCount { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime UpdatedTime { get; set; }
}
