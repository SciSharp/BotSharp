using BotSharp.Abstraction.Knowledges.Models;

namespace BotSharp.Abstraction.Knowledges;

public interface IKnowledgeHook
{
    Task<List<KnowledgeChunk>> CollectChunkedKnowledge();
}
