namespace BotSharp.Abstraction.Knowledges.Models;

public class KnowledgeFilter : StringIdPagination
{
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    [JsonPropertyName("with_vector")]
    public bool WithVector { get; set; }
}
