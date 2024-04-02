using BotSharp.Abstraction.Conversations.Enums;

namespace BotSharp.Abstraction.Conversations.Models;

public class StateKeyValue
{
    public string Key { get; set; }
    public bool Versioning { get; set; }
    public List<StateValue> Values { get; set; } = new List<StateValue>();

    public StateKeyValue()
    {

    }

    public StateKeyValue(string key, List<StateValue> values)
    {
        Key = key;
        Values = values;
    }

    public override string ToString()
    {
        var lastValue = Values.LastOrDefault();
        return $"{Key} => ({lastValue?.ToString()})";
    }
}

public class StateValue
{
    public string Data { get; set; }

    [JsonPropertyName("message_id")]
    public string? MessageId { get; set; }

    public bool Active { get; set; }

    [JsonPropertyName("active_rounds")]
    public int ActiveRounds { get; set; }

    [JsonPropertyName("data_type")]
    public string DataType { get; set; } = StateDataType.String;

    [JsonPropertyName("source")]
    public string Source { get; set; }

    [JsonPropertyName("update_time")]
    public DateTime UpdateTime { get; set; }

    public StateValue()
    {

    }

    public override string ToString()
    {
        var isActive = Active ? "Yes" : "No";
        var activeRounds = ActiveRounds <= 0 ? "infinity" : ActiveRounds.ToString();
        return $"Data: {Data}, Active: {isActive}, Active rounds: {activeRounds}, Source: {Source}";
    }
}