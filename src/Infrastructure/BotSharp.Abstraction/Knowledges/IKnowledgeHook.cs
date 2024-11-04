namespace BotSharp.Abstraction.Knowledges;

public interface IKnowledgeHook
{
    Task<List<KnowledgeChunk>> CollectChunkedKnowledge()
        => Task.FromResult(new List<KnowledgeChunk>());

    Task<List<string>> GetDomainKnowledges(RoleDialogModel message, string text)
        => Task.FromResult(new List<string>());

    Task<List<string>> GetGlobalKnowledges(RoleDialogModel message)
        => Task.FromResult(new List<string>());
}
