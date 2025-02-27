namespace BotSharp.Plugin.KnowledgeBase.Hooks;

public class KnowledgeHook : IKnowledgeHook
{
    private readonly IKnowledgeService _knowledgeService;
    private readonly ITextEmbedding _textEmbedding;
    private readonly IServiceProvider _services;

    public KnowledgeHook(
        IKnowledgeService knowledgeService,
        ITextEmbedding textEmbedding,
        IServiceProvider services)
    {
        _knowledgeService = knowledgeService;
        _textEmbedding = textEmbedding;
        _services = services;
    }

    private async Task<List<AgentKnowledgeBase>> GetKnowledgeBaseNameByAgentIdAsync(string agentId)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.GetAgent(agentId);
        return agent.KnowledgeBases;
    }

    public async Task<List<string>> GetDomainKnowledges(RoleDialogModel message, string text)
    {
        // Get agent Id by knowledge base name
        var knowledgeBases = await GetKnowledgeBaseNameByAgentIdAsync(message.CurrentAgentId);
        var results = new List<string>();

        foreach (var knowledgeBase in knowledgeBases)
        {
            if (knowledgeBase.Type == "relationships")
            {
                var options = new GraphSearchOptions
                {
                    Method = "local"
                };
                var result = await _knowledgeService.SearchGraphKnowledge(text, options);
                results.Add(result.Result);
            }
            else if (knowledgeBase.Type == "document")
            {
                var options = new VectorSearchOptions
                {
                    Fields = null,
                    Limit = 5,
                    Confidence = 0.25f,
                    WithVector = true
                };
                var result = await _knowledgeService.SearchVectorKnowledge(text, knowledgeBase.Name, options);
                results.AddRange(result.Where(x => x.Data != null && x.Data.ContainsKey("text"))
                               .Select(x => x.Data["text"].ToString())
                               .Where(x => x != null)!);
            }
            else
            {
                var options = new VectorSearchOptions
                {
                    Fields = null,
                    Limit = 5,
                    Confidence = 0.5f,
                    WithVector = true
                };
                var result = await _knowledgeService.SearchVectorKnowledge(text, knowledgeBase.Name, options);
                results.AddRange(result.Where(x => x.Data != null && (x.Data.ContainsKey("text") || x.Data.ContainsKey("answer")))
                               .Select(x => x.Data.ContainsKey("answer") ? x.Data["text"].ToString() + "\r\n\r\n" + x.Data["answer"].ToString() : x.Data["text"].ToString())
                               .Where(x => x != null)!);

            }
        }

        return results;
    }

    public async Task<List<string>> GetGlobalKnowledges(RoleDialogModel message)
    {
        var text = message.Content;
        var results = new List<string>();

        // Get all knowledge bases
        var knowledgeBases = await _knowledgeService.GetVectorCollections();

        foreach (var knowledgeBase in knowledgeBases)
        {
            if (knowledgeBase.Type == "relationships")
            {
                var options = new GraphSearchOptions
                {
                    Method = "local"
                };
                var result = await _knowledgeService.SearchGraphKnowledge(text, options);
                results.Add(result.Result);
            }
            else
            {
                var options = new VectorSearchOptions
                {
                    Fields = null,
                    Limit = 5,
                    Confidence = 0.5f,
                    WithVector = true
                };
                var result = await _knowledgeService.SearchVectorKnowledge(text, knowledgeBase.Name, options);
                results.AddRange(result.Where(x => x.Data != null && (x.Data.ContainsKey("text") || x.Data.ContainsKey("answer")))
                               .Select(x => x.Data.ContainsKey("answer") ? x.Data["text"].ToString() + "\r\n\r\n" + x.Data["answer"].ToString() : x.Data["text"].ToString())
                               .Where(x => x != null)!);
            }
        }

        return results;
    }

    public async Task<List<KnowledgeChunk>> CollectChunkedKnowledge()
    {
        return new List<KnowledgeChunk>();
    }
}