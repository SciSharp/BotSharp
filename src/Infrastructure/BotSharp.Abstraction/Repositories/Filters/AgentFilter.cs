namespace BotSharp.Abstraction.Repositories.Filters;

public class AgentFilter
{
    public string? AgentName { get; set; }
    public bool? Disabled { get; set; }
    public bool? AllowRouting { get; set; }
    public bool? IsPublic { get; set; }
    public List<string>? AgentIds { get; set; }
}
