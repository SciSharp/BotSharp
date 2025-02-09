using BotSharp.Abstraction.Statistics.Enums;

namespace BotSharp.Abstraction.Statistics.Models;

public class BotSharpStatsInput
{
    public string Metric { get; set; }
    public string Dimension { get; set; }
    public string DimRefVal { get; set; }
    public List<StatsKeyValuePair> Data { get; set; } = [];
    public DateTime RecordTime { get; set; } = DateTime.UtcNow;
    public StatsInterval IntervalType { get; set; } = StatsInterval.Day;
}
