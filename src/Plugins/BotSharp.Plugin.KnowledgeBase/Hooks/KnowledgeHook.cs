using BotSharp.Abstraction.Graph.Options;

namespace BotSharp.Plugin.KnowledgeBase.Hooks;

public class KnowledgeHook : IKnowledgeHook
{
    private readonly IServiceProvider _services;
    private readonly IGraphKnowledgeService _graphKnowledgeService;
    

    public KnowledgeHook(
        IServiceProvider services,
        IGraphKnowledgeService graphKnowledgeService)
    {
        _services = services;
        _graphKnowledgeService = graphKnowledgeService;
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
                var options = new GraphQueryOptions
                {
                    Provider = "Remote",
                    Method = "local"
                };
                var result = await _graphKnowledgeService.ExecuteQueryAsync(text, options);
                results.Add(result.Result);
            }
            else if (knowledgeBase.Type == "document")
            {
                var orchestrator = GetKnowledgeOrchestrator(knowledgeBase.Type);
                var options = new KnowledgeSearchOptions
                {
                    Fields = null,
                    Limit = 5,
                    Confidence = 0.25f,
                    WithVector = true
                };
                var result = await orchestrator.Search(text, knowledgeBase.Name, options);
                results.AddRange(result.Where(x => x.Data != null && x.Data.ContainsKey("text"))
                               .Select(x => x.Data["text"].ToString())
                               .Where(x => x != null)!);
            }
            else
            {
                var orchestrator = GetKnowledgeOrchestrator(knowledgeBase.Type);
                var options = new KnowledgeSearchOptions
                {
                    Fields = null,
                    Limit = 5,
                    Confidence = 0.5f,
                    WithVector = true
                };
                var result = await orchestrator.Search(text, knowledgeBase.Name, options);
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
        var orchestrator = GetKnowledgeOrchestrator();
        var knowledgeBases = await orchestrator.GetCollections(new KnowledgeCollectionOptions { IncludeAllTypes = true });

        foreach (var knowledgeBase in knowledgeBases)
        {
            if (knowledgeBase.Type == "relationships")
            {
                var options = new GraphQueryOptions
                {
                    Provider = "Remote",
                    Method = "local"
                };
                var result = await _graphKnowledgeService.ExecuteQueryAsync(text, options);
                results.Add(result.Result);
            }
            else
            {
                var searchOrchestrator = GetKnowledgeOrchestrator(knowledgeBase.Type);
                var options = new KnowledgeSearchOptions
                {
                    Fields = null,
                    Limit = 5,
                    Confidence = 0.5f,
                    WithVector = true
                };
                var result = await searchOrchestrator.Search(text, knowledgeBase.Name, options);
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

    private IKnowledgeOrchestrator GetKnowledgeOrchestrator(string? type = null)
    {
        var orchestrators = _services.GetServices<IKnowledgeOrchestrator>();
        if (!string.IsNullOrWhiteSpace(type))
        {
            return orchestrators.First(x => x.KnowledgeType == type);
        }
        return orchestrators.First();
    }
}