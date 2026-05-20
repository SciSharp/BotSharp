using BotSharp.Abstraction.VectorStorage.Models;

namespace BotSharp.Abstraction.Knowledges.Models;

public class KnowledgeCollectionDetails
{
    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("payload_schema")]
    public List<PayloadSchemaDetail> PayloadSchema { get; set; } = [];
}
