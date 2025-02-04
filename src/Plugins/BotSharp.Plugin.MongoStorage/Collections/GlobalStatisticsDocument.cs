namespace BotSharp.Plugin.MongoStorage.Collections;

public class GlobalStatisticsDocument : MongoBase
{
    public string Metric { get; set; }
    public string Dimension { get; set; }
    public string DimRefVal { get; set; }
    public IDictionary<string, double> Data { get; set; } = new Dictionary<string, double>();
    public DateTime RecordTime { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Interval { get; set; }
}
