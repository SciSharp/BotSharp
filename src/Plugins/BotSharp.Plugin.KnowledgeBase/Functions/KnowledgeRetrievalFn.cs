namespace BotSharp.Plugin.KnowledgeBase.Functions;

public class KnowledgeRetrievalFn : IFunctionCallback
{
    public string Name => "knowledge_retrieval";

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

        var embedding =  _services.GetServices<ITextEmbedding>()
            .FirstOrDefault(x => x.GetType().FullName.EndsWith(_settings.TextEmbedding));

        var vector = await embedding.GetVectorAsync(args.Question);
        var vectorDb = _services.GetRequiredService<IVectorDb>();
        var knowledges = await vectorDb.Search(KnowledgeCollectionName.BotSharp, vector, KnowledgePayloadName.Answer);

        if (knowledges.Count > 0)
        {
            message.Content = string.Join("\r\n\r\n=====\r\n", knowledges);
        }
        else
        {
            message.Content = $"I didn't find any useful knowledge related to [{args.Question}]. \r\nCan you tell me the instruction and I'll memorize it.";
            message.StopCompletion = true;
        }

        return true;
    }
}
