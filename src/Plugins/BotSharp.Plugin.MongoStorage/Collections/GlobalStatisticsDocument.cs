namespace BotSharp.Plugin.MongoStorage.Collections;

public class GlobalStatisticsDocument : MongoBase
{
    public string Category { get; set; }
    public string Group { get; set; }
    public IDictionary<string, double> Data { get; set; } = new Dictionary<string, double>();
    public DateTime RecordTime { get; set; }
}
