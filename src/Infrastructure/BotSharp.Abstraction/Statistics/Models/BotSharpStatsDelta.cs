using BotSharp.Abstraction.Statistics.Enums;

namespace BotSharp.Abstraction.Statistics.Models;

public class BotSharpStatsDelta
{
    public string AgentId { get; set; } = null!;
    public StatsCountDelta CountDelta { get; set; } = new();
    public StatsLlmCostDelta LlmCostDelta { get; set; } = new();
    public DateTime RecordTime { get; set; } = DateTime.UtcNow;
    public StatsInterval IntervalType { get; set; } = StatsInterval.Day;

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
}

public class StatsCountDelta
{
    public int AgentCallCountDelta { get; set; }
}

public class StatsLlmCostDelta
{
    public int PromptTokensDelta { get; set; }
    public int CompletionTokensDelta { get; set; }
    public float PromptTotalCostDelta { get; set; }
    public float CompletionTotalCostDelta { get; set; }
}
