namespace BotSharp.Plugin.SqlDriver.Hooks;

public class SqlDriverKnowledgeHook : IKnowledgeHook
{
    public async Task<List<KnowledgeChunk>> CollectChunkedKnowledge()
    {
        return new List<KnowledgeChunk>();
    }
}
