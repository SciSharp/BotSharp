namespace BotSharp.Abstraction.Knowledges.Models;

public class KnowledgeFilter : StringIdPagination
{
    [JsonPropertyName("with_vector")]
    public bool WithVector { get; set; }
}
