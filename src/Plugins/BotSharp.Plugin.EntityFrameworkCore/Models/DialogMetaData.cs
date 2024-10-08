using System;

namespace BotSharp.Plugin.EntityFrameworkCore.Entities;

public class DialogMetaData
{
    public string Role { get; set; }
    public string AgentId { get; set; }
    public string MessageId { get; set; }
    public string? FunctionName { get; set; }
    public string? SenderId { get; set; }
    public DateTime CreateTime { get; set; }
}
