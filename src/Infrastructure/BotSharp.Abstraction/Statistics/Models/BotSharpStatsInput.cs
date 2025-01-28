namespace BotSharp.Abstraction.Statistics.Models;

public class BotSharpStatsInput
{
    public string Category { get; set; }
    public string Group { get; set; }
    public List<StatsKeyValuePair> Data { get; set; } = [];
    public DateTime RecordTime { get; set; }
}
