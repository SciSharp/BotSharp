namespace BotSharp.Abstraction.Models;

public class KeyValue
{
    [JsonPropertyName("key")]
    public string Key { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }

    public override string ToString()
    {
        return $"Key: {Key}, Value: {Value}";
    }
}
