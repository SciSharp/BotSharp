namespace BotSharp.Abstraction.Repositories.Filters;

public class AgentFilter
{
    public Pagination Pager { get; set; } = new Pagination();
    public string? AgentName { get; set; }
    public bool? Disabled { get; set; }
    public bool? Installed { get; set; }
    public string? Type { get; set; }
    public bool? IsPublic { get; set; }
    public List<string>? AgentIds { get; set; }
}
