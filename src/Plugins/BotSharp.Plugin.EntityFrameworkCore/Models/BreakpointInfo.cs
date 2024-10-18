using System;

namespace BotSharp.Plugin.EntityFrameworkCore.Entities;

public class BreakpointInfo
{
    public string? MessageId { get; set; }
    public DateTime Breakpoint { get; set; }
    public DateTime CreatedTime { get; set; }
    public string? Reason { get; set; }
}
