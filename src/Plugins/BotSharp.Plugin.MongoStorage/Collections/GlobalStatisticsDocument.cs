namespace BotSharp.Plugin.MongoStorage.Collections;

public class GlobalStatisticsDocument : MongoBase
{
    public string Category { get; set; }
    public string Group { get; set; }
    public IDictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    public DateTime RecordTime { get; set; }
}
