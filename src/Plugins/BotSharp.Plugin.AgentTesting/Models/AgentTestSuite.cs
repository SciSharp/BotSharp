namespace BotSharp.Plugin.AgentTesting.Models;

public class AgentTestSuite : MongoBase
{
    public string AgentId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Model used by llmJudge. When unconfigured, llmJudge assertions fail outright rather than
    /// passing silently.
    /// </summary>
    public string? JudgeProvider { get; set; }
    public string? JudgeModel { get; set; }

    /// <summary>Functions let through on top of the default control-flow allow list.</summary>
    public List<string> ExtraAllowedFunctions { get; set; } = [];

    /// <summary>Functions blocked outright; wins over the allow list.</summary>
    public List<string> ForceBlockedFunctions { get; set; } = [];

    public int CaseTimeoutSeconds { get; set; } = 120;

    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdateDate { get; set; } = DateTime.UtcNow;
}
