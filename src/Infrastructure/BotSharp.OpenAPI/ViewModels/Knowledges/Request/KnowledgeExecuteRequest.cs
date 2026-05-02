using BotSharp.Abstraction.VectorStorage.Models;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class KnowledgeExecuteRequest : KnowledgeBaseRequestBase
{
    public string Text { get; set; } = string.Empty;

    public IEnumerable<string>? Fields { get; set; }

    public IEnumerable<VectorFilterGroup>? FilterGroups { get; set; }

    public int? Limit { get; set; } = 5;

    public float? Confidence { get; set; } = 0.5f;

    public bool WithVector { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object?>? SearchParam { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object?>? SearchArguments { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? DataProviders { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxNgram { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? GraphId { get; set; }
}
