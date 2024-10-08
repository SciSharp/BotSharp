namespace BotSharp.Plugin.EntityFrameworkCore.Entities;

public class AgentLlmConfig
{
    public string? Provider { get; set; }
    public string? Model { get; set; }
    public bool IsInherit { get; set; }
    public int MaxRecursionDepth { get; set; }
}
