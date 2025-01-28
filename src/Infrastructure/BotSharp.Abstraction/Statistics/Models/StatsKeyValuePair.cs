using BotSharp.Abstraction.Statistics.Enums;

namespace BotSharp.Abstraction.Statistics.Models;

public class StatsKeyValuePair
{
    public string Key { get; set; }
    public double Value { get; set; }
    public StatsOperation Operation { get; set; }

    public StatsKeyValuePair()
    {
        
    }

    public StatsKeyValuePair(string key, double value, StatsOperation operation = StatsOperation.Add)
    {
        Key = key;
        Value = value;
        Operation = operation;
    }

    public override string ToString()
    {
        return $"[{Key}]: {Value} ({Operation})";
    }
}