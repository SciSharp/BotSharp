using BotSharp.Abstraction.VectorStorage.Models;

namespace BotSharp.Abstraction.Knowledges.Options;

public class KnowledgeExecuteOptions
{
    public string? DbProvider { get; set; }
    public IEnumerable<string>? Fields { get; set; }
    public IEnumerable<VectorFilterGroup>? FilterGroups { get; set; }
    public Dictionary<string, string?>? SearchParam { get; set; }
    public Dictionary<string, object>? SearchArguments { get; set; }

    public int? Limit { get; set; } = 5;
    public float? Confidence { get; set; } = 0.5f;
    public bool WithVector { get; set; }
}
