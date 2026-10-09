namespace BotSharp.Abstraction.Repositories.Filters;

public class AgentFilter
{
    public Pagination Pager { get; set; } = new Pagination();
    public List<string>? AgentIds { get; set; }
    public List<string>? AgentNames { get; set; }
    public string? SimilarName { get; set; }
    public bool? Disabled { get; set; }
    public bool? Installed { get; set; }
    public List<string>? Types { get; set; }
    public List<string>? Labels { get; set; }
    public bool? IsPublic { get; set; }

    public static AgentFilter Empty()
    {
        return new AgentFilter();
    }
}
