namespace BotSharp.Plugin.KnowledgeBase.Functions;

public class KnowledgeRetrievalFn : IFunctionCallback
{
    public string Name => "knowledge_retrieval";

    public string Indication => "searching my brain";

    private readonly IServiceProvider _services;
    private readonly KnowledgeBaseSettings _settings;

    public KnowledgeRetrievalFn(IServiceProvider services, KnowledgeBaseSettings settings)
    {
        _services = services;
        _settings = settings;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<ExtractedKnowledge>(message.FunctionArgs ?? "{}");

        // Get knowledge from vectordb
        var hooks = _services.GetServices<IKnowledgeHook>();
        var knowledges = new List<string>();
        foreach (var hook in hooks)
        {
            var k = await hook.GetRelevantKnowledges(message, args.Question);
            knowledges.AddRange(k);
        }
        knowledges = knowledges.Distinct().ToList();

        if (!knowledges.IsNullOrEmpty())
        {
            message.Content = string.Join("\r\n\r\n=====\r\n", knowledges);
        }
        else
        {
            message.Content = $"I didn't find any useful knowledge related to [{args.Question}].";
        }

        return true;
    }
}
