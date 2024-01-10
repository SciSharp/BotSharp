using BotSharp.Abstraction.Functions;
using BotSharp.Plugin.KnowledgeBase.LlmContexts;

namespace BotSharp.Plugin.KnowledgeBase.Functions;

public class SearchKnowledgesFn : IFunctionCallback
{
    public string Name => "search_knowledges";
    private readonly IServiceProvider _services;

    public SearchKnowledgesFn(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<KnowledgeContextIn>(message.FunctionArgs);

        var knowledgeService = _services.GetRequiredService<IKnowledgeService>();
        var knowledge = await knowledgeService.GetKnowledges(new KnowledgeRetrievalModel
        {
            AgentId = message.CurrentAgentId,
            Question = args.Question
        });

        if (string.IsNullOrEmpty(knowledge))
        {
            message.Content = "Can't find any relevant data in local knowledge base.";
            message.UnmatchedAgent = true;
        }

        return true;
    }
}
