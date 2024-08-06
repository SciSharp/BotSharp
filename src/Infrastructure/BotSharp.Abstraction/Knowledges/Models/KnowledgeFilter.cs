namespace BotSharp.Abstraction.Knowledges.Models;

public class KnowledgeFilter : UuidPagination
{
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    [JsonPropertyName("with_vector")]
    public bool WithVector { get; set; }
}
