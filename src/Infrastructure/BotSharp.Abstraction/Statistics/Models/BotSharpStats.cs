using BotSharp.Abstraction.Statistics.Enums;

namespace BotSharp.Abstraction.Statistics.Models;

public class BotSharpStats
{
    [JsonPropertyName("metric")]
    public string Metric { get; set; } = null!;

    [JsonPropertyName("dimension")]
    public string Dimension { get; set; } = null!;

    [JsonPropertyName("dim_ref_val")]
    public string DimRefVal { get; set; } = null!;

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
        return $"{Metric}-{Dimension}-{DimRefVal} ({Interval}): {Data?.Count ?? 0}";
    }

    public static (DateTime, DateTime) BuildTimeInterval(DateTime recordTime, StatsInterval interval)
    {
        DateTime startTime = recordTime;
        DateTime endTime = startTime;

        switch (interval)
        {
            case StatsInterval.Minute:
                startTime = new DateTime(recordTime.Year, recordTime.Month, recordTime.Day, recordTime.Hour, recordTime.Minute, 0);
                endTime = startTime.AddMinutes(1);
                break;
            case StatsInterval.Hour:
                startTime = new DateTime(recordTime.Year, recordTime.Month, recordTime.Day, recordTime.Hour, 0, 0);
                endTime = startTime.AddHours(1);
                break;
            default:
                startTime = new DateTime(recordTime.Year, recordTime.Month, recordTime.Day, 0, 0, 0);
                endTime = startTime.AddDays(1);
                break;
        }

        endTime = endTime.AddSeconds(-1);
        startTime = DateTime.SpecifyKind(startTime, DateTimeKind.Utc);
        endTime = DateTime.SpecifyKind(endTime, DateTimeKind.Utc);
        return (startTime, endTime);
    }
}