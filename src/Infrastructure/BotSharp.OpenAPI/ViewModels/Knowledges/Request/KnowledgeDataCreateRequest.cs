using BotSharp.Abstraction.VectorStorage.Models;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class KnowledgeDataCreateRequest : KnowledgeBaseRequestBase
{
    public string Text { get; set; }

    public Dictionary<string, VectorPayloadValue>? Payload { get; set; }
}
