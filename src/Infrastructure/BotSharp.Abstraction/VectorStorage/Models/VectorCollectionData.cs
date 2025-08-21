namespace BotSharp.Abstraction.VectorStorage.Models;

public class VectorCollectionData
{
    public string Id { get; set; }
    public Dictionary<string, VectorPayloadValue> Data { get; set; } = new();
    public Dictionary<string, object> Payload => Data?.ToDictionary(x => x.Key, x => x.Value.DataValue) ?? [];
    public double? Score { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float[]? Vector { get; set; }
}