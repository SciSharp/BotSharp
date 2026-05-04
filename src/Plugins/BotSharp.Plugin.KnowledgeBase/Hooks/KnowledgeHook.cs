namespace BotSharp.Plugin.KnowledgeBase.Hooks;

public class KnowledgeHook : IKnowledgeHook
{
    private readonly IServiceProvider _services;

    public KnowledgeHook(
        IServiceProvider services)
    {
        _services = services;
    }

    public async Task<List<string>> GetDomainKnowledges(RoleDialogModel message, string text)
    {
        // Get agent Id by knowledge base name
        var knowledgeBases = await GetKnowledgeBaseNameByAgentIdAsync(message.CurrentAgentId);
        var results = new List<string>();

        foreach (var knowledgeBase in knowledgeBases)
        {
            if (knowledgeBase.Type == KnowledgeBaseType.Document)
            {
                var kg = GetKnowledgeService(knowledgeBase.Type);
                var options = new KnowledgeExecuteOptions
                {
                    Fields = null,
                    Limit = 5,
                    Confidence = 0.25f,
                    WithVector = true
                };
                var result = await kg.ExecuteQuery(text, knowledgeBase.Name, options);
                results.AddRange(result.Where(x => x.Data != null && x.Data.ContainsKey("text"))
                               .Select(x => x.Data["text"].ToString())
                               .Where(x => x != null)!);
            }
            else if (knowledgeBase.Type == KnowledgeBaseType.QuestionAnswer)
            {
                var kg = GetKnowledgeService(knowledgeBase.Type);
                var options = new KnowledgeExecuteOptions
                {
                    Fields = null,
                    Limit = 5,
                    Confidence = 0.5f,
                    WithVector = true
                };
                var result = await kg.ExecuteQuery(text, knowledgeBase.Name, options);
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
        var kg = GetKnowledgeService();
        var knowledgeBases = await kg.GetCollections(new() { IncludeAllTypes = true });

        foreach (var knowledgeBase in knowledgeBases)
        {
            if (knowledgeBase.Type == KnowledgeBaseType.Document
                || knowledgeBase.Type == KnowledgeBaseType.QuestionAnswer)
            {
                var kgSearcher = GetKnowledgeService(knowledgeBase.Type);
                var options = new KnowledgeExecuteOptions
                {
                    Fields = null,
                    Limit = 5,
                    Confidence = 0.5f,
                    WithVector = true
                };
                var result = await kgSearcher.ExecuteQuery(text, knowledgeBase.Name, options);
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


    private async Task<List<AgentKnowledgeBase>> GetKnowledgeBaseNameByAgentIdAsync(string agentId)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.GetAgent(agentId);
        return agent.KnowledgeBases;
    }

    private IKnowledgeService GetKnowledgeService(string? type = null)
    {
        var kgs = _services.GetServices<IKnowledgeService>();
        if (!string.IsNullOrWhiteSpace(type))
        {
            return kgs.First(x => x.KnowledgeType == type);
        }
        return kgs.First();
    }
}