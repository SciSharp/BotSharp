using BotSharp.Abstraction.Knowledges.Options;
using BotSharp.Abstraction.VectorStorage.Models;

namespace BotSharp.Abstraction.Knowledges.Models;

public class KnowledgeCreateModel : KnowledgeOptionBase
{
    public string Text { get; set; }
    public Dictionary<string, VectorPayloadValue>? Payload { get; set; }
}
