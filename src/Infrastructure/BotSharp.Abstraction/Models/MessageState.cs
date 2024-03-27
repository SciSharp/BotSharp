namespace BotSharp.Abstraction.Models;

public class MessageState
{
    public string Key { get; set; }
    public string Value { get; set; }

    [JsonPropertyName("active_rounds")]
    public int ActiveRounds { get; set; } = -1;

    public MessageState()
    {
        
    }

    public MessageState(string key, string value, int activeRounds = -1)
    {
        Key = key;
        Value = value;
        ActiveRounds = activeRounds;
    }
}
