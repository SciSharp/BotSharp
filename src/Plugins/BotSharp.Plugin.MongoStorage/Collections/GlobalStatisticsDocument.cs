namespace BotSharp.Plugin.MongoStorage.Collections;

public class GlobalStatisticsDocument : MongoBase
{
    public string AgentId { get; set; } = null!;
    public int AgentCallCount { get; set; }
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public float PromptTotalCost { get; set; }
    public float CompletionTotalCost { get; set; }

    public DateTime RecordTime { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Interval { get; set; } = default!;
}
