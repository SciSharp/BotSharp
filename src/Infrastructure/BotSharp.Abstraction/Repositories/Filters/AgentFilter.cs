using BotSharp.Abstraction.Infrastructures;

namespace BotSharp.Abstraction.Repositories.Filters;

public class AgentFilter : ICacheKey
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

    /// <summary>
    /// Content-based, so equal filters share one cache entry. RuleEngine builds a new filter on every
    /// trigger; keyed by identity, each trigger cached another full copy of every agent.
    /// Every filter field must be part of the key, or two different filters would share a result.
    /// </summary>
    public string GetCacheKey()
        => $"{nameof(AgentFilter)}_{Pager?.GetCacheKey()}_{JoinKey(AgentIds)}_{JoinKey(AgentNames)}_{SimilarName}" +
           $"_{Disabled}_{Installed}_{JoinKey(Types)}_{JoinKey(Labels)}_{IsPublic}";

    // Joined with the ASCII unit separator, which is not expected in agent ids, names, types or labels.
    private static string JoinKey(List<string>? values)
        => values == null ? "null" : $"[{string.Join('\u001f', values)}]";
}
