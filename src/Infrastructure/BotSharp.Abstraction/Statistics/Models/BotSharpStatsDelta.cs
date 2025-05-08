using BotSharp.Abstraction.Statistics.Enums;

namespace BotSharp.Abstraction.Statistics.Models;

public class BotSharpStatsDelta
{
    public string AgentId { get; set; } = null!;
    public int AgentCallCountDelta { get; set; }
    public int PromptTokensDelta { get; set; }
    public int CompletionTokensDelta { get; set; }
    public float PromptTotalCostDelta { get; set; }
    public float CompletionTotalCostDelta { get; set; }
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
