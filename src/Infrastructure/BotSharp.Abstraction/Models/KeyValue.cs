namespace BotSharp.Abstraction.Models;

public class KeyValue
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string? Value { get; set; }

    public override string ToString()
    {
        return $"Key: {Key}, Value: {Value}";
    }
}

public class KeyValue<T>
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public T? Value { get; set; }

    public override string ToString()
    {
        return $"Key: {Key}, Value: {Value}";
    }
}
