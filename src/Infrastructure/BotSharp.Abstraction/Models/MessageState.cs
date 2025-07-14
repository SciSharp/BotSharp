namespace BotSharp.Abstraction.Models;

public class MessageState
{
    public string Key { get; set; }
    public object Value { get; set; }

    [JsonPropertyName("active_rounds")]
    public int ActiveRounds { get; set; } = -1;

    [JsonPropertyName("global")]
    public bool Global { get; set; }

    public MessageState()
    {
        
    }

    public MessageState(string key, object value, int activeRounds = -1, bool global = false)
    {
        Key = key;
        Value = value;
        ActiveRounds = activeRounds;
        Global = global;
    }

    public override string ToString()
    {
        return $"Key: {Key} => Value: {Value}, ActiveRounds: {ActiveRounds}, Global: {Global}";
    }
}
