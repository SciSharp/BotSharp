using BotSharp.Abstraction.Statistics.Enums;

namespace BotSharp.Abstraction.Statistics.Models;

public class BotSharpStats
{
    [JsonPropertyName("metric")]
    public string Metric { get; set; } = null!;

    [JsonPropertyName("dimension")]
    public string Dimension { get; set; } = null!;

    [JsonPropertyName("data")]
    public IDictionary<string, double> Data { get; set; } = new Dictionary<string, double>();

    [JsonPropertyName("record_time")]
    public DateTime RecordTime { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public StatsInterval IntervalType { get; set; }

    [JsonPropertyName("interval")]
    public string Interval
    {
        get
        {
            return IntervalType.ToString();
        } 
        set
        {
            if (Enum.TryParse(value, out StatsInterval type))
            {
                IntervalType = type;
            }
        }
    }

    [JsonPropertyName("start_time")]
    public DateTime StartTime { get; set; }

    [JsonPropertyName("end_time")]
    public DateTime EndTime { get; set; }

    public override string ToString()
    {
        return $"{Metric}-{Dimension} ({Interval}): {Data?.Count ?? 0}";
    }
}