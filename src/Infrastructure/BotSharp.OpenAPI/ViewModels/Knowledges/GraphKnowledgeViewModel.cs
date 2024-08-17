using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class GraphKnowledgeViewModel
{
    [JsonPropertyName("result")]
    public string Result { get; set; }
}
