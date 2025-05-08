using BotSharp.Abstraction.Statistics.Enums;

namespace BotSharp.Abstraction.Statistics.Models;

public class BotSharpStats
{
    [JsonPropertyName("agent_id")]
    public string AgentId { get; set; } = null!;

    [JsonPropertyName("count")]
    public StatsCount Count { get; set; } = new();

    [JsonPropertyName("llm_cost")]
    public StatsLlmCost LlmCost { get; set; } = new();

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
        return $"Stats: {AgentId}-{IntervalType}";
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

public class StatsCount
{
    [JsonPropertyName("agent_call_count")]
    public long AgentCallCount { get; set; }
}

public class StatsLlmCost
{
    [JsonPropertyName("prompt_tokens")]
    public long PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public long CompletionTokens { get; set; }

    [JsonPropertyName("prompt_total_cost")]
    public float PromptTotalCost { get; set; }

    [JsonPropertyName("completion_total_cost")]
    public float CompletionTotalCost { get; set; }
}