namespace BotSharp.Plugin.MongoStorage.Collections;

public class GlobalStatisticsDocument : MongoBase
{
    public string AgentId { get; set; } = null!;
    public StatsCountMongoElement Count { get; set; } = new();
    public StatsLlmCostMongoElement LlmCost { get; set; } = new();

    public DateTime RecordTime { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Interval { get; set; } = default!;
}
