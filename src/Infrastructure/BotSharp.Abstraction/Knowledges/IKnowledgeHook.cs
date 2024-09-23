namespace BotSharp.Abstraction.Knowledges;

public interface IKnowledgeHook
{
    Task<List<KnowledgeChunk>> CollectChunkedKnowledge()
        => Task.FromResult(new List<KnowledgeChunk>());

    Task<List<string>> GetRelevantKnowledges(string text)
        => Task.FromResult(new List<string>());

    Task<List<string>> GetGlobalKnowledges()
        => Task.FromResult(new List<string>());
}
