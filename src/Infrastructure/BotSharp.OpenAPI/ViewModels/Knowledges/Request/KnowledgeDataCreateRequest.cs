using BotSharp.Abstraction.VectorStorage.Models;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class KnowledgeDataCreateRequest
{
    public string Text { get; set; }

    public Dictionary<string, VectorPayloadValue>? Payload { get; set; }

    public string KnowledgeType { get; set; } = null!;

    public string? DbProvider { get; set; }
}
