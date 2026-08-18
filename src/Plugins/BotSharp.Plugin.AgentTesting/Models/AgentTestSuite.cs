namespace BotSharp.Plugin.AgentTesting.Models;

public class AgentTestSuite : MongoBase
{
    public string AgentId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool Enabled { get; set; } = true;

    /// <summary>llmJudge 用的模型；未配置时 llmJudge 断言直接失败，不静默通过。</summary>
    public string? JudgeProvider { get; set; }
    public string? JudgeModel { get; set; }

    /// <summary>在默认控制流白名单之外额外放行的函数。</summary>
    public List<string> ExtraAllowedFunctions { get; set; } = [];

    /// <summary>强制阻断的函数，优先级高于白名单。</summary>
    public List<string> ForceBlockedFunctions { get; set; } = [];

    public int CaseTimeoutSeconds { get; set; } = 120;

    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdateDate { get; set; } = DateTime.UtcNow;
}
