using BotSharp.Abstraction.Knowledges;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Plugin.KnowledgeBase.Services;
using Microsoft.Extensions.DependencyInjection;
using Tensorflow;

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

        // if (string.IsNullOrEmpty(agentId))
        // {
        //     return settings.DefaultKnowledgeBase;
        // }

        // return settings.AgentKnowledgeBaseMap.TryGetValue(agentId, out var kbName)
        //     ? kbName
        //     : settings.DefaultKnowledgeBase;
    }

    public async Task<List<string>> GetDomainKnowledges(RoleDialogModel message, string text)
    {


        // 根据当前 agent ID 获取对应的知识库名称
        var knowledgeBases = await GetKnowledgeBaseNameByAgentIdAsync(message.CurrentAgentId);
        var results = new List<string>();

        foreach (var knowledgeBase in knowledgeBases)
        {
            // if(knowledgeBase.Type=="")
            // {
            //     var result = await _knowledgeService.SearchVectorKnowledge(text, knowledgeBase.Name, options);
            //     results.AddRange(result);

            // }
            if (knowledgeBase.Type == "relationships")
            {
                var options=new GraphSearchOptions{
                    Method="local"
                };
                var result = await _knowledgeService.SearchGraphKnowledge(text, options);
                results.Add(result.Result);
            }
            else// if(knowledgeBase.Type=="relationships")
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

        // 从向量数据库中检索相关内容
        // var results = await _knowledgeService.SearchVectorKnowledge(text, knowledgeBases, options);

        return results;//.Select(x => x.Data["text"].ToString()).ToList();
    }

    public async Task<List<string>> GetGlobalKnowledges(RoleDialogModel message)
    {
        // return new List<string>();

        //便利所有的知识库
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
        // // 从消息内容中获取向量
        // var vector = await _textEmbedding.GetEmbeddingAsync(message.Content);

        // // 从向量数据库中检索相关内容
        // var results = await _knowledgeService.SearchKnowledgeAsync(vector, 3);

        // return results.Select(x => x.Content).ToList();
    }

    public async Task<List<KnowledgeChunk>> CollectChunkedKnowledge()
    {
        // 如果需要收集和分块知识，可以在这里实现
        return new List<KnowledgeChunk>();//<KnowledgeChunk>();
    }
}