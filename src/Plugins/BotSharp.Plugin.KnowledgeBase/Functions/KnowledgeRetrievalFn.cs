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

        var embedding =  _services.GetServices<ITextEmbedding>().FirstOrDefault(x => x.Provider == _settings.TextEmbedding.Provider);
        embedding.SetModelName(_settings.TextEmbedding.Model);

        var vector = await embedding.GetVectorAsync(args.Question);
        var vectorDb = _services.GetServices<IVectorDb>().FirstOrDefault(x => x.Name == _settings.VectorDb);
        var collectionName = !string.IsNullOrWhiteSpace(_settings.DefaultCollection) ? _settings.DefaultCollection : KnowledgeCollectionName.BotSharp;
        var knowledges = await vectorDb.Search(collectionName, vector, new List<string> { KnowledgePayloadName.Text, KnowledgePayloadName.Answer });

        if (!knowledges.IsNullOrEmpty())
        {
            var answers = knowledges.Select(x => $"Question: {x.Data[KnowledgePayloadName.Text]}\r\nAnswer: {x.Data[KnowledgePayloadName.Answer]}").ToList();
            message.Content = string.Join("\r\n\r\n=====\r\n", answers);
        }
        else
        {
            message.Content = $"I didn't find any useful knowledge related to [{args.Question}]. \r\nCan you tell me the instruction and I'll memorize it.";
            message.StopCompletion = true;
        }

        return true;
    }
}
