using BotSharp.Abstraction.VectorStorage.Models;

namespace BotSharp.Abstraction.Knowledges.Models;

public class KnowledgeCollectionData
{
    public string Id { get; set; }
    public Dictionary<string, VectorPayloadValue> Payload { get; set; } = new();
    public Dictionary<string, object> Data => Payload?.ToDictionary(x => x.Key, x => x.Value.DataValue) ?? [];
    public double? Score { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float[]? Vector { get; set; }
}
