namespace BotSharp.Plugin.MongoStorage.Collections;

public class GlobalStatisticsDocument : MongoBase
{
    public string Metric { get; set; }
    public string Dimension { get; set; }
    public IDictionary<string, double> Data { get; set; } = new Dictionary<string, double>();
    public DateTime RecordTime { get; set; }
}
