using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class CreateCollectionRequest
{
    public string CollectionName { get; set; }
    public string KnowledgeType { get; set; }
    public string Provider { get; set; }
    public string Model { get; set; }
    public int Dimension { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DbProvider { get; set; }
}
