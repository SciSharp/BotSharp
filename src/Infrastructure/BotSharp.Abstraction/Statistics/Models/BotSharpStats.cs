namespace BotSharp.Abstraction.Statistics.Models;

public class BotSharpStats
{
    [JsonPropertyName("metric")]
    public string Metric { get; set; } = null!;

    [JsonPropertyName("dimension")]
    public string Dimension { get; set; } = null!;

    [JsonPropertyName("data")]
    public IDictionary<string, double> Data { get; set; } = new Dictionary<string, double>();

    private DateTime innerRecordTime;

    [JsonPropertyName("record_time")]
    public DateTime RecordTime
    {
        get
        {
            return innerRecordTime;
        }
        set
        {
            var date = new DateTime(value.Year, value.Month, value.Day, value.Hour, 0, 0);
            innerRecordTime = DateTime.SpecifyKind(date, DateTimeKind.Utc);
        }
    }

    public override string ToString()
    {
        return $"{Metric}-{Dimension}: {Data?.Count ?? 0} ({RecordTime})";
    }
}