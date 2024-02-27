using BotSharp.Abstraction.Knowledges.Models;

namespace BotSharp.Abstraction.Knowledges;

public interface IKnowledgeHook
{
    Task<List<KnowledgeChunk>> CollectChunkedKnowledge()
        => Task.FromResult(new List<KnowledgeChunk>());

    Task<List<string>> GetRelevantKnowledges()
        => Task.FromResult(new List<string>());
}
