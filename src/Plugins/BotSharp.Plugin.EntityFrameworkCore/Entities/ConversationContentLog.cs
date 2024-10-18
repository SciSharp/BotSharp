using System;

namespace BotSharp.Plugin.EntityFrameworkCore.Entities;

public class ConversationContentLog
{
    public string Id { get; set; }
    public string ConversationId { get; set; }
    public string MessageId { get; set; }
    public string? Name { get; set; }
    public string? AgentId { get; set; }
    public string Role { get; set; }
    public string Source { get; set; }
    public string Content { get; set; }
    public DateTime CreatedTime { get; set; }
}
